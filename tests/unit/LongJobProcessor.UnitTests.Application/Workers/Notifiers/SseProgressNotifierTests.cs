using FluentAssertions;
using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Workers.Notifiers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace LongJobProcessor.UnitTests.Application.Workers.Notifiers;

public class SseProgressNotifierTests
{
    private readonly Mock<IConnectionManager> _connectionManagerMock;
    private readonly Mock<ILogger<SseProgressNotifier>> _loggerMock;
    private readonly SseProgressNotifier _notifier;

    public SseProgressNotifierTests()
    {
        _connectionManagerMock = new Mock<IConnectionManager>();
        _loggerMock = new Mock<ILogger<SseProgressNotifier>>();
        _notifier = new SseProgressNotifier(_loggerMock.Object, _connectionManagerMock.Object);
    }

    [Fact]
    public async Task NotifyProgressAsync_WhenConnectionExists_ShouldSendProgress()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var progressData = "test progress";
        var cancellationToken = CancellationToken.None;
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = false };

        _connectionManagerMock.Setup(x => x.GetConnection(userId, jobId))
            .Returns(writer);

        // Act
        await _notifier.NotifyProgressAsync(userId, jobId, progressData, cancellationToken);

        // Assert
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("data:");
        content.Should().Contain(jobId.ToString());
        content.Should().Contain("progress");
    }

    [Fact]
    public async Task NotifyProgressAsync_WhenConnectionDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var progressData = "test progress";
        var cancellationToken = CancellationToken.None;

        _connectionManagerMock.Setup(x => x.GetConnection(userId, jobId))
            .Returns((StreamWriter?)null);

        // Act
        var act = async () => await _notifier.NotifyProgressAsync(userId, jobId, progressData, cancellationToken);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task NotifyProgressAsync_WhenExceptionOccurs_ShouldUnregisterConnection()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var progressData = "test progress";
        var cancellationToken = CancellationToken.None;
        var writer = new Mock<StreamWriter>(new MemoryStream());
        writer.Setup(x => x.WriteLineAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        _connectionManagerMock.Setup(x => x.GetConnection(userId, jobId))
            .Returns(writer.Object);

        // Act
        await _notifier.NotifyProgressAsync(userId, jobId, progressData, cancellationToken);

        // Assert
        _connectionManagerMock.Verify(x => x.UnregisterConnection(userId, jobId), Times.Once);
    }

    [Fact]
    public async Task NotifyStatusAsync_WhenConnectionExists_ShouldSendStatus()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var status = "Running";
        var cancellationToken = CancellationToken.None;
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = false };

        _connectionManagerMock.Setup(x => x.GetConnection(userId, jobId))
            .Returns(writer);

        // Act
        await _notifier.NotifyStatusAsync(userId, jobId, status, cancellationToken);

        // Assert
        stream.Position = 0;
        var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("data:");
        content.Should().Contain(jobId.ToString());
        content.Should().Contain("status");
        content.Should().Contain(status);
    }
}

