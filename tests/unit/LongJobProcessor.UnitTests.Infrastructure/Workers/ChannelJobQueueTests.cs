using FluentAssertions;
using LongJobProcessor.Infrastructure.Workers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LongJobProcessor.UnitTests.Infrastructure.Workers;

public class ChannelJobQueueTests
{
    private readonly Mock<ILogger<ChannelJobQueue>> _loggerMock;
    private readonly ChannelJobQueue _queue;

    public ChannelJobQueueTests()
    {
        _loggerMock = new Mock<ILogger<ChannelJobQueue>>();
        _queue = new ChannelJobQueue(_loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldEnqueueJobId()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        // Act
        await _queue.EnqueueAsync(jobId, cancellationToken);

        // Assert - No exception should be thrown
    }

    [Fact]
    public async Task DequeueAsync_ShouldReturnEnqueuedJobId()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        // Act
        var enqueueTask = _queue.EnqueueAsync(jobId, cancellationToken);
        var dequeueTask = _queue.DequeueAsync(cancellationToken);

        await Task.WhenAll(enqueueTask, dequeueTask);
        var dequeuedId = await dequeueTask;

        // Assert
        dequeuedId.Should().Be(jobId);
    }

    [Fact]
    public async Task DequeueAsync_ShouldWaitForEnqueuedItems()
    {
        // Arrange
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        // Act
        await _queue.EnqueueAsync(jobId1, cancellationToken);
        await _queue.EnqueueAsync(jobId2, cancellationToken);

        var dequeuedId1 = await _queue.DequeueAsync(cancellationToken);
        var dequeuedId2 = await _queue.DequeueAsync(cancellationToken);

        // Assert
        dequeuedId1.Should().Be(jobId1);
        dequeuedId2.Should().Be(jobId2);
    }

    [Fact]
    public async Task DequeueAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _queue.DequeueAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

