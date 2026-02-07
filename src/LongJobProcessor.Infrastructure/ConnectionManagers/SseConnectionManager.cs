using LongJobProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LongJobProcessor.Infrastructure.ConnectionManagers;

public class SseConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, StreamWriter>> _connections;
    private readonly ILogger<SseConnectionManager> _logger;

    public SseConnectionManager(ILogger<SseConnectionManager> logger)
    {
        _connections = new();
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

    public StreamWriter? GetConnection(string userId, Guid jobId)
    {
        if (_connections.TryGetValue(userId, out var userConnections))
        {
            userConnections.TryGetValue(jobId, out var writer);
            return writer;
        }

        return null;
    }
}
