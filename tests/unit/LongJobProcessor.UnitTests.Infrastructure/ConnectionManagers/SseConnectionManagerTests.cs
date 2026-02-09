using FluentAssertions;
using LongJobProcessor.Infrastructure.ConnectionManagers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace LongJobProcessor.UnitTests.Infrastructure.ConnectionManagers;

public class SseConnectionManagerTests
{
    private readonly Mock<ILogger<SseConnectionManager>> _loggerMock;
    private readonly SseConnectionManager _manager;

    public SseConnectionManagerTests()
    {
        _loggerMock = new Mock<ILogger<SseConnectionManager>>();
        _manager = new SseConnectionManager(_loggerMock.Object);
    }

    [Fact]
    public void RegisterConnection_ShouldStoreConnection()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var writer = new StreamWriter(new MemoryStream(), leaveOpen: true);

        // Act
        _manager.RegisterConnection(userId, jobId, writer);

        // Assert
        var retrieved = _manager.GetConnection(userId, jobId);
        retrieved.Should().Be(writer);
    }

    [Fact]
    public void GetConnection_WhenNotRegistered_ShouldReturnNull()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();

        // Act
        var result = _manager.GetConnection(userId, jobId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UnregisterConnection_ShouldRemoveConnection()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var writer = new StreamWriter(new MemoryStream(), leaveOpen: true);
        _manager.RegisterConnection(userId, jobId, writer);

        // Act
        _manager.UnregisterConnection(userId, jobId);

        // Assert
        var result = _manager.GetConnection(userId, jobId);
        result.Should().BeNull();
    }

    [Fact]
    public void RegisterConnection_WithMultipleJobs_ShouldStoreAll()
    {
        // Arrange
        var userId = "user-1";
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();
        var writer1 = new StreamWriter(new MemoryStream(), leaveOpen: true);
        var writer2 = new StreamWriter(new MemoryStream(), leaveOpen: true);

        // Act
        _manager.RegisterConnection(userId, jobId1, writer1);
        _manager.RegisterConnection(userId, jobId2, writer2);

        // Assert
        _manager.GetConnection(userId, jobId1).Should().Be(writer1);
        _manager.GetConnection(userId, jobId2).Should().Be(writer2);
    }

    [Fact]
    public void UnregisterConnection_WhenLastJob_ShouldRemoveUser()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var writer = new StreamWriter(new MemoryStream(), leaveOpen: true);
        _manager.RegisterConnection(userId, jobId, writer);

        // Act
        _manager.UnregisterConnection(userId, jobId);

        // Assert
        var result = _manager.GetConnection(userId, jobId);
        result.Should().BeNull();
    }
}

