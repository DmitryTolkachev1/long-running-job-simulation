using LongJobProcessor.Application.Common;
using LongJobProcessor.Application.Workers.Notifiers;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LongJobProcessor.Application.Jobs.Commands.ConnectToJob;

public sealed class ConnectToJobHandler : IRequestHandler<ConnectToJobQuery, BaseResponse>
{
    private readonly SseProgressNotifier _progressNotifier;
    private readonly ILogger<ConnectToJobHandler> _logger;

    public ConnectToJobHandler(SseProgressNotifier progressNotifier, ILogger<ConnectToJobHandler> logger)
    {
        _progressNotifier = progressNotifier;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(ConnectToJobQuery request, CancellationToken cancellationToken)
    {
        var writer = request.Writer;
        
        _progressNotifier.RegisterConnection(request.UserId, request.JobId, writer);

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
            _progressNotifier.UnregisterConnection(request.UserId, request.JobId);
            await writer.DisposeAsync();
        }

        return new();
    }
}
