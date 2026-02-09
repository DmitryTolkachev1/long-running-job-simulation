using FluentAssertions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using Xunit;

namespace LongJobProcessor.UnitTests.Domain.Entities;

public class JobTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCreatedStatus()
    {
        // Arrange
        var userId = "user-1";

        // Act
        var job = new InputEncodeJob(userId) { Input = "test" };

        // Assert
        job.UserId.Should().Be(userId);
        job.Status.Should().Be(JobStatus.Created);
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Enqueue_ShouldTransitionToQueued()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };

        // Act
        job.Enqueue();

        // Assert
        job.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public void Start_ShouldTransitionToRunningAndSetStartedAt()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        job.Enqueue();
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        job.State.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));

        // Act
        job.Start(workerId);

        // Assert
        job.Status.Should().Be(JobStatus.Running);
        job.StartedAt.Should().NotBeNull();
        job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }


    [Fact]
    public void RequestCancel_ShouldTransitionToCancelledOrCancelling()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        job.Enqueue();

        // Act
        job.RequestCancel();

        // Assert
        job.Status.Should().Be(JobStatus.Cancelled);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldTransitionToCompletedAndSetCompletedAt()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        job.Enqueue();
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        job.State.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        job.Start(workerId);

        // Act
        job.Complete(workerId);

        // Assert
        job.Status.Should().Be(JobStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Fail_ShouldTransitionToFailedAndSetCompletedAt()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        job.Enqueue();
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        job.State.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        job.Start(workerId);

        // Act
        job.Fail(workerId);

        // Assert
        job.Status.Should().Be(JobStatus.Failed);
        job.CompletedAt.Should().NotBeNull();
    }
}

