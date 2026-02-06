using LongJobProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LongJobProcessor.Application.Workers.Notifiers;

public sealed class SseProgressNotifier : IProgressNotifier
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, StreamWriter>> _connections = new();
    private readonly ILogger<SseProgressNotifier> _logger;

    public SseProgressNotifier(ILogger<SseProgressNotifier> logger)
    {
        _logger = logger;
    }

    public void RegisterConnection(string userId, Guid jobId, StreamWriter writer)
    {
        var userConnections = _connections.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, StreamWriter>());
        userConnections[jobId] = writer;
        _logger.LogDebug($"Registered SSE connection for user {userId}, job {jobId}");
    }

    public void UnregisterConnection(string userId, Guid jobId)
    {
        if (_connections.TryGetValue(userId, out var userConnections))
        {
            userConnections.TryRemove(jobId, out _);
            if (userConnections.IsEmpty)
            {
                _connections.TryRemove(userId, out _);
            }
            _logger.LogDebug($"Unregistered SSE connection for user {userId}, job {jobId}");
        }
    }

    public async Task NotifyProgressAsync(string userId, Guid jobId, object data, CancellationToken cancellationToken)
    {
        try
        {
            var writer = GetConnection(userId, jobId);
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
            UnregisterConnection(userId, jobId);
        }
    }

    public async Task NotifyStatusAsync(string userId, Guid jobId, string status, CancellationToken cancellationToken)
    {
        try
        {
            var writer = GetConnection(userId, jobId);
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
            UnregisterConnection(userId, jobId);
        }
    }

    private StreamWriter? GetConnection(string userId, Guid jobId)
    {
        if (_connections.TryGetValue(userId, out var userConnections))
        {
            userConnections.TryGetValue(jobId, out var writer);
            return writer;
        }

        return null;
    }
}
