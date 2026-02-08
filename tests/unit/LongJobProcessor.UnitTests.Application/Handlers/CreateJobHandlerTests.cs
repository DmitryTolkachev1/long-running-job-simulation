using FluentAssertions;
using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LongJobProcessor.UnitTests.Application.Handlers;

public class CreateJobHandlerTests
{
    private readonly Mock<IJobFactory> _jobFactoryMock;
    private readonly Mock<IJobRepository<Job>> _jobRepositoryMock;
    private readonly Mock<IJobQueue> _jobQueueMock;
    private readonly Mock<ILogger<CreateJobHandler>> _loggerMock;
    private readonly CreateJobHandler _handler;

    public CreateJobHandlerTests()
    {
        _jobFactoryMock = new Mock<IJobFactory>();
        _jobRepositoryMock = new Mock<IJobRepository<Job>>();
        _jobQueueMock = new Mock<IJobQueue>();
        _loggerMock = new Mock<ILogger<CreateJobHandler>>();
        _handler = new CreateJobHandler(
            _jobFactoryMock.Object,
            _jobRepositoryMock.Object,
            _jobQueueMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateJobAndEnqueue()
    {
        // Arrange
        var userId = "user-1";
        var jobType = JobType.Encode;
        var jobData = new Dictionary<string, object> { { "Input", "test input" } };
        var command = new CreateJobCommand(userId, jobType, jobData);
        var job = new Domain.Entities.Jobs.InputEncode.InputEncodeJob(userId) { Input = "test input" };
        var cancellationToken = CancellationToken.None;

        _jobFactoryMock.Setup(x => x.CreateJob(jobType, userId, jobData)).Returns(job);
        _jobRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Job>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _jobQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<Guid>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.JobId.Should().Be(job.Id);
        _jobFactoryMock.Verify(x => x.CreateJob(jobType, userId, jobData), Times.Once);
        _jobRepositoryMock.Verify(x => x.AddAsync(It.Is<Job>(j => j.Id == job.Id), cancellationToken), Times.Once);
        _jobQueueMock.Verify(x => x.EnqueueAsync(job.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldEnqueueJobAfterCreation()
    {
        // Arrange
        var userId = "user-1";
        var jobType = JobType.Encode;
        var jobData = new Dictionary<string, object> { { "Input", "test input" } };
        var command = new CreateJobCommand(userId, jobType, jobData);
        var job = new Domain.Entities.Jobs.InputEncode.InputEncodeJob(userId) { Input = "test input" };
        var cancellationToken = CancellationToken.None;

        _jobFactoryMock.Setup(x => x.CreateJob(jobType, userId, jobData)).Returns(job);
        _jobRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Job>(), cancellationToken))
            .Returns(Task.CompletedTask);
        _jobQueueMock.Setup(x => x.EnqueueAsync(It.IsAny<Guid>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        job.Status.Should().Be(JobStatus.Queued);
    }
}

