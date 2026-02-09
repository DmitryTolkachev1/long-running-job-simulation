using LongJobProcessor.API.Authentication;
using LongJobProcessor.API.Workers;
using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Behaviors;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Application.Workers;
using LongJobProcessor.Application.Workers.Notifiers;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Infrastructure.ConnectionManagers;
using LongJobProcessor.Infrastructure.Jobs;
using LongJobProcessor.Infrastructure.Workers;
using LongJobProcessor.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LongJobProcessor.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:9000", "http://localhost:4200"];

        builder.Services.AddAuthentication("Basic")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", options => { });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowedSpecificOrigin", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<IJobRepository<Job>, JobRepository>();
        builder.Services.AddScoped<IJobFactory, JobFactory>();

        builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();

        builder.Services.AddSingleton<IJobExecutor, InputEncodeJobExecutor>();

        builder.Services.AddSingleton<IConnectionManager, SseConnectionManager>();
        builder.Services.AddSingleton<IProgressNotifier, SseProgressNotifier>();

        builder.Services.AddHostedService<JobWorker>();
        builder.Services.AddHostedService<JobCleanupWorker>();

        builder.Services.AddControllers();

        builder.Services.AddSwaggerGen();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<CreateJobCommand>();
        });
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseCors("AllowedSpecificOrigin");

        app.MapControllers();

        app.MapGet("/api/health", (IWebHostEnvironment env) =>
        {
            var response = new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                environment = env.EnvironmentName
            };

            return Results.Json(response);
        });

        app.Run();
    }
}
