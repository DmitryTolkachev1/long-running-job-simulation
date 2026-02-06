using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Behaviors;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Application.Workers;
using LongJobProcessor.Application.Workers.Notifiers;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Infrastructure.Jobs;
using LongJobProcessor.Infrastructure.Workers;
using LongJobProcessor.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LongJobProcessor.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") ?? builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString);
        });

        builder.Services.AddScoped<IJobRepository<Job>, JobRepository>();
        builder.Services.AddScoped<IJobFactory, JobFactory>();

        builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();

        builder.Services.AddSingleton<IJobExecutor, InputEncodeJobExecutor>();

        builder.Services.AddSingleton<IProgressNotifier, SseProgressNotifier>();
        builder.Services.AddSingleton<SseProgressNotifier>();

        builder.Services.AddHostedService<JobWorker>();

        builder.Services.AddControllers(options =>
        {
            options.ReturnHttpNotAcceptable = false;
        });

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

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
