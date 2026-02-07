using LongJobProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LongJobProcessor.Application.Workers.Notifiers;

public sealed class SseProgressNotifier : IProgressNotifier
{
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SseProgressNotifier> _logger;

    public SseProgressNotifier(ILogger<SseProgressNotifier> logger, IConnectionManager connectionManager)
    {
        _logger = logger;
        _connectionManager = connectionManager;
    }

    public async Task NotifyProgressAsync(string userId, Guid jobId, object data, CancellationToken cancellationToken)
    {
        try
        {
            var writer = _connectionManager.GetConnection(userId, jobId);
            if (writer != null)
            {
                var progressData = JsonSerializer.Serialize(new { jobId, payload = data, type = "progress" });
                await writer.WriteLineAsync($"data: {progressData}");
                await writer.WriteLineAsync();
                await writer.FlushAsync(cancellationToken);

                _logger.LogTrace($"Sent progress update for job {jobId} to user {userId}. Data: {progressData}");
            }
            else
            {
                _logger.LogTrace($"No SSE connection found for user {userId}, {jobId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send progress update for job {jobId} to user {userId}");
            _connectionManager.UnregisterConnection(userId, jobId);
        }
    }

    public async Task NotifyStatusAsync(string userId, Guid jobId, string status, CancellationToken cancellationToken)
    {
        try
        {
            var writer = _connectionManager.GetConnection(userId, jobId);
            if (writer != null)
            {
                var progressData = JsonSerializer.Serialize(new { jobId, status, type = "status" });
                await writer.WriteLineAsync($"data: {progressData}");
                await writer.WriteLineAsync();
                await writer.FlushAsync(cancellationToken);

                _logger.LogTrace($"Sent status update for job {jobId} to user {userId}. Status: {status}");
            }
            else
            {
                _logger.LogTrace($"No SSE connection found for user {userId}, {jobId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send progress update for job {jobId} to user {userId}");
            _connectionManager.UnregisterConnection(userId, jobId);
        }
    }
}
