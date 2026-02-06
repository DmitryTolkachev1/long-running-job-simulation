using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
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

    public Task<Job?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Jobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken)
    {

        _dbContext.Jobs.Update(job);
        await _dbContext.SaveChangesAsync();
    }
}
