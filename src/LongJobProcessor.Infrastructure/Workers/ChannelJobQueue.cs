using LongJobProcessor.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace LongJobProcessor.Infrastructure.Workers;

public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel;
    private readonly ILogger<ChannelJobQueue> _logger;

    public ChannelJobQueue(ILogger<ChannelJobQueue> logger)
    {
        _logger = logger;

        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Guid>(options);
    }

    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        var jobId = await _channel.Reader.ReadAsync(cancellationToken);
        _logger.LogDebug($"Dequeued job {jobId}");
        return jobId;
    }

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(jobId, cancellationToken);
        _logger.LogDebug($"Enqueued job {jobId}");
    }
}
