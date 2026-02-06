using LongJobProcessor.Domain.Entities.Jobs;

namespace LongJobProcessor.Application.Abstractions;

public interface IProgressNotifier
{
    Task NotifyStatusAsync(string userId, Guid jobId, string status, CancellationToken cancellationToken);

    Task NotifyProgressAsync(string userId, Guid jobId, object data, CancellationToken cancellationToken);
}
