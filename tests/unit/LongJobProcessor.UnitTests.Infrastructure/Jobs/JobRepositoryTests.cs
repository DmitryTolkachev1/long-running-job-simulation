using FluentAssertions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Infrastructure.Jobs;
using LongJobProcessor.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LongJobProcessor.UnitTests.Infrastructure.Jobs;

public class JobRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly JobRepository _repository;

    public JobRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);
        _repository = new JobRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldAddJobToDatabase()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        await _repository.AddAsync(job, cancellationToken);

        // Assert
        var savedJob = await _dbContext.Jobs.FindAsync(job.Id);
        savedJob.Should().NotBeNull();
        savedJob!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetAsync_WhenJobExists_ShouldReturnJob()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        await _repository.AddAsync(job, CancellationToken.None);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _repository.GetAsync(job.Id, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.UserId.Should().Be(job.UserId);
    }

    [Fact]
    public async Task GetAsync_WhenJobDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _repository.GetAsync(jobId, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsNoTrackingAsync_ShouldReturnJobWithoutTracking()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        await _repository.AddAsync(job, CancellationToken.None);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _repository.GetAsNoTrackingAsync(job.Id, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        _dbContext.Entry(result).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateJob()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        await _repository.AddAsync(job, CancellationToken.None);
        job.Enqueue();
        var cancellationToken = CancellationToken.None;

        // Act
        await _repository.UpdateAsync(job, cancellationToken);

        // Assert
        var updatedJob = await _repository.GetAsync(job.Id, cancellationToken);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnJobsWithMatchingStatus()
    {
        // Arrange
        var job1 = new InputEncodeJob("user-1") { Input = "test1" };
        var job2 = new InputEncodeJob("user-2") { Input = "test2" };
        await _repository.AddAsync(job1, CancellationToken.None);
        await _repository.AddAsync(job2, CancellationToken.None);
        job1.Enqueue();
        await _repository.UpdateAsync(job1, CancellationToken.None);
        var cancellationToken = CancellationToken.None;

        // Act
        var queuedJobs = await _repository.GetByStatusAsync(JobStatus.Queued, cancellationToken);

        // Assert
        queuedJobs.Should().Contain(j => j.Id == job1.Id);
        queuedJobs.Should().NotContain(j => j.Id == job2.Id);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

