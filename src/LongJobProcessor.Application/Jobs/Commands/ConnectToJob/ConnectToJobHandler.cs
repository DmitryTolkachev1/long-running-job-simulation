using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Common;
using LongJobProcessor.Application.Workers.Notifiers;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LongJobProcessor.Application.Jobs.Commands.ConnectToJob;

public sealed class ConnectToJobHandler : IRequestHandler<ConnectToJobQuery, BaseResponse>
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<ConnectToJobHandler> _logger;
    private readonly IJobRepository<Job> _jobRepository;

    public ConnectToJobHandler(IConnectionManager connectionManager, ILogger<ConnectToJobHandler> logger, IJobRepository<Job> jobRepository)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _jobRepository = jobRepository;
    }

    public async Task<BaseResponse> Handle(ConnectToJobQuery request, CancellationToken cancellationToken)
    {
        var writer = request.Writer;

        _connectionManager.RegisterConnection(request.UserId, request.JobId, writer);

        try
        {
            var data = JsonSerializer.Serialize(new {request.JobId, type = "connected", message = "SSE connected" });
            var initialMessage = $"data: {data}";
            await writer.WriteLineAsync(initialMessage);
            await writer.WriteLineAsync();
            await writer.FlushAsync(cancellationToken);

            _logger.LogDebug($"SSE connetion established for job {request.JobId}");

            while (!cancellationToken.IsCancellationRequested)
            {
                var job = await _jobRepository.GetAsNoTrackingAsync(request.JobId, cancellationToken);
                if (job is null || job.Status is JobStatus.Cancelled or JobStatus.Cancelling or JobStatus.Completed)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                var keepAlive = "keep-alive";
                await writer.WriteLineAsync(keepAlive);
                await writer.WriteLineAsync();
                await writer.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug($"SSE connetion closed for job {request.JobId}");
        }
        finally
        {
            _connectionManager.UnregisterConnection(request.UserId, request.JobId);
            await writer.DisposeAsync();
        }

        return new();
    }
}
