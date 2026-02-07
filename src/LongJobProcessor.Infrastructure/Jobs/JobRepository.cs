using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LongJobProcessor.Infrastructure.Jobs;

public class JobRepository : IJobRepository<Job>
{
    private readonly AppDbContext _dbContext;
    public JobRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken)
    {
        await _dbContext.Jobs.AddAsync(job, cancellationToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Job?> GetAsNoTrackingAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Jobs
               .AsNoTracking()
               .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task<Job?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Jobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Job>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken)
    {
        return await _dbContext.Jobs
               .Where(job => job.State.Status == status)
               .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken)
    {

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }
}
