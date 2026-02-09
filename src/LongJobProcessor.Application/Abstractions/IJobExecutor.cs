using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Application.Abstractions;

public interface IJobExecutor
{
    JobType JobType { get; }
    Task ExecuteAsync(Job job, Func<object, Task> progressCallback, CancellationToken cancellationToken);
}
