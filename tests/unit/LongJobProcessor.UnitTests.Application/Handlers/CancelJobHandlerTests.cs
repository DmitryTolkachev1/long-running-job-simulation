using FluentAssertions;
using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Jobs.Commands.CancelJob;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LongJobProcessor.UnitTests.Application.Handlers;

public class CancelJobHandlerTests
{
    private readonly Mock<IJobRepository<Job>> _jobRepositoryMock;
    private readonly Mock<ILogger<CancelJobHandler>> _loggerMock;
    private readonly CancelJobHandler _handler;

    public CancelJobHandlerTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository<Job>>();
        _loggerMock = new Mock<ILogger<CancelJobHandler>>();
        _handler = new CancelJobHandler(_jobRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenJobExists_ShouldCancelJob()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var job = new InputEncodeJob(userId) { Input = "test", Id = jobId };
        job.Enqueue();
        var command = new CancelJobCommand(userId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsNoTrackingAsync(jobId, cancellationToken))
            .ReturnsAsync(job);
        _jobRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Job>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Cancelled);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Job>(j => j.Id == jobId), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenJobNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var command = new CancelJobCommand(userId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsNoTrackingAsync(jobId, cancellationToken))
            .ReturnsAsync((Job?)null);

        // Act
        var act = async () => await _handler.Handle(command, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<JobNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserMismatch_ShouldThrowException()
    {
        // Arrange
        var userId = "user-1";
        var otherUserId = "user-2";
        var jobId = Guid.NewGuid();
        var job = new InputEncodeJob(userId) { Input = "test" };
        var command = new CancelJobCommand(otherUserId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsNoTrackingAsync(jobId, cancellationToken))
            .ReturnsAsync(job);

        // Act
        var act = async () => await _handler.Handle(command, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<UserMismatchException>();
    }
}

