using FluentAssertions;
using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Jobs.Queries.GetJobState;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LongJobProcessor.UnitTests.Application.Handlers;

public class GetJobStateHandlerTests
{
    private readonly Mock<IJobRepository<Job>> _jobRepositoryMock;
    private readonly Mock<ILogger<GetJobStateHandler>> _loggerMock;
    private readonly GetJobStateHandler _handler;

    public GetJobStateHandlerTests()
    {
        _jobRepositoryMock = new Mock<IJobRepository<Job>>();
        _loggerMock = new Mock<ILogger<GetJobStateHandler>>();
        _handler = new GetJobStateHandler(_jobRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenJobExists_ShouldReturnJobState()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var job = new InputEncodeJob(userId) { Input = "test", Id = jobId };
        var query = new GetJobStateQuery(userId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsync(jobId, cancellationToken))
            .ReturnsAsync(job);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.JobId.Should().Be(jobId);
        result.JobStatus.Should().Be(job.Status.ToString());
        result.CreatedAt.Should().Be(job.CreatedAt);
        result.StartedAt.Should().Be(job.StartedAt);
        result.CompletedAt.Should().Be(job.CompletedAt);
    }

    [Fact]
    public async Task Handle_WhenJobNotFound_ShouldThrowException()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var query = new GetJobStateQuery(userId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsync(jobId, cancellationToken))
            .ReturnsAsync((Job?)null);

        // Act
        var act = async () => await _handler.Handle(query, cancellationToken);

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
        var query = new GetJobStateQuery(otherUserId, jobId);
        var cancellationToken = CancellationToken.None;

        _jobRepositoryMock.Setup(x => x.GetAsync(jobId, cancellationToken))
            .ReturnsAsync(job);

        // Act
        var act = async () => await _handler.Handle(query, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<UserMismatchException>();
    }
}

