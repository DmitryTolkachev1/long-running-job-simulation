using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Application.Abstractions;

public interface IJobRepository<TJob> where TJob : Job
{
    Task<TJob?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<TJob>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken);
    Task<TJob?> GetAsNoTrackingAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(TJob job, CancellationToken cancellationToken);
    Task UpdateAsync(TJob job, CancellationToken cancellationToken);
}
