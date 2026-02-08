using FluentAssertions;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Domain.Exceptions;
using LongJobProcessor.Domain.StateMachine;
using Xunit;

namespace LongJobProcessor.UnitTests.Domain.StateMachine;

public class JobStateMachineTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCreatedStatus()
    {
        // Arrange & Act
        var stateMachine = new JobStateMachine();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Created);
        stateMachine.JobOwner.Should().BeNull();
        stateMachine.TakenUntil.Should().BeNull();
        stateMachine.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithInitialStatus_ShouldSetStatus()
    {
        // Arrange & Act
        var stateMachine = new JobStateMachine(JobStatus.Queued);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public void Enqueue_FromCreated_ShouldTransitionToQueued()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Created);

        // Act
        stateMachine.Enqueue();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Queued);
    }

    [Fact]
    public void Enqueue_FromNonCreatedStatus_ShouldThrowException()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);

        // Act
        var act = () => stateMachine.Enqueue();

        // Assert
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void TryAcquireJob_FromQueued_ShouldAcquireJob()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var result = stateMachine.TryAcquireJob(workerId, now, duration);

        // Assert
        result.Should().BeTrue();
        stateMachine.Status.Should().Be(JobStatus.Taken);
        stateMachine.JobOwner.Should().Be(workerId);
        stateMachine.TakenUntil.Should().BeCloseTo(now.Add(duration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TryAcquireJob_FromRetrying_ShouldAcquireJob()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Retrying);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var result = stateMachine.TryAcquireJob(workerId, now, duration);

        // Assert
        result.Should().BeTrue();
        stateMachine.Status.Should().Be(JobStatus.Taken);
    }

    [Fact]
    public void TryAcquireJob_FromNonQueuedOrRetrying_ShouldReturnFalse()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Running);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var result = stateMachine.TryAcquireJob(workerId, now, duration);

        // Assert
        result.Should().BeFalse();
        stateMachine.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public void Start_FromTaken_ShouldTransitionToRunning()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));

        // Act
        stateMachine.Start(workerId);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public void Start_WithWrongOwner_ShouldThrowException()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));

        // Act
        var act = () => stateMachine.Start("wrong-worker");

        // Assert
        act.Should().Throw<OwnerMismatchException>();
    }

    [Fact]
    public void Heartbeat_ShouldUpdateTakenUntil()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        var newNow = now.AddMinutes(2);
        var newDuration = TimeSpan.FromMinutes(5);

        // Act
        stateMachine.Heartbeat(workerId, newNow, newDuration);

        // Assert
        stateMachine.TakenUntil.Should().BeCloseTo(newNow.Add(newDuration), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RequestCancel_FromQueued_ShouldTransitionToCancelled()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);

        // Act
        stateMachine.RequestCancel();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Cancelled);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void RequestCancel_FromTaken_ShouldTransitionToCancelling()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));

        // Act
        stateMachine.RequestCancel();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Cancelling);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void RequestCancel_FromRunning_ShouldTransitionToCancelling()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);

        // Act
        stateMachine.RequestCancel();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Cancelling);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void Retry_FromAbandoned_ShouldTransitionToRetrying()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Abandoned);

        // Act
        stateMachine.Retry();

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Retrying);
    }

    [Fact]
    public void Complete_FromRunning_ShouldTransitionToCompleted()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);

        // Act
        stateMachine.Complete(workerId);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Completed);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void Fail_FromRunning_ShouldTransitionToFailed()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);

        // Act
        stateMachine.Fail(workerId);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Failed);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void CheckOwnershipExpired_WhenNotExpired_ShouldNotChangeStatus()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);
        var checkTime = now.AddMinutes(2);

        // Act
        stateMachine.CheckOwnershipExpired(checkTime, 3);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Running);
    }

    [Fact]
    public void CheckOwnershipExpired_WhenExpiredAndRetryCountExceeded_ShouldTransitionToFailed()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);
        
        for (int i = 0; i < 3; i++)
        {
            var checkTime = now.AddMinutes(10 + i * 10);
            stateMachine.CheckOwnershipExpired(checkTime, 3);
            if (stateMachine.Status == JobStatus.Retrying)
            {
                stateMachine.TryAcquireJob(workerId, checkTime.AddMinutes(1), TimeSpan.FromMinutes(5));
                stateMachine.Start(workerId);
            }
        }
        
        var finalCheckTime = now.AddMinutes(50);
        stateMachine.CheckOwnershipExpired(finalCheckTime, 3);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Failed);
        stateMachine.RetryCount.Should().BeGreaterThan(3);
    }

    [Fact]
    public void CancelStuck_WhenOwnerIsNull_ShouldCancel()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Cancelling);
        var now = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);

        // Act
        stateMachine.CancelStuck(now, duration);

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Cancelled);
        stateMachine.JobOwner.Should().BeNull();
    }

    [Fact]
    public void CancelStuck_WhenTakenUntilExpired_ShouldCancel()
    {
        // Arrange
        var stateMachine = new JobStateMachine(JobStatus.Queued);
        var workerId = "worker-1";
        var now = DateTime.UtcNow;
        stateMachine.TryAcquireJob(workerId, now, TimeSpan.FromMinutes(5));
        stateMachine.Start(workerId);
        stateMachine.RequestCancel();
        
        var stuckTime = now.AddMinutes(20);

        // Act
        stateMachine.CancelStuck(stuckTime, TimeSpan.FromMinutes(5));

        // Assert
        stateMachine.Status.Should().Be(JobStatus.Cancelled);
    }
}

