using LongJobProcessor.Domain.Entities.Jobs;

namespace LongJobProcessor.Application.Abstractions;

public interface IJobRepository<TJob> where TJob : Job
{
    Task<TJob?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(TJob job, CancellationToken cancellationToken);
    Task UpdateAsync(TJob job, CancellationToken cancellationToken);
}
