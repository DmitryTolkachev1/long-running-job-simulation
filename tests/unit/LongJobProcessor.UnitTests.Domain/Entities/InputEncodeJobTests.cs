using FluentAssertions;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using Xunit;

namespace LongJobProcessor.UnitTests.Domain.Entities;

public class InputEncodeJobTests
{
    [Fact]
    public void UpdateProgress_ShouldUpdateCursorAndProduced()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        var cursor = 5;
        var produced = "abcde";

        // Act
        job.UpdateProgress(cursor, produced);

        // Assert
        job.Cursor.Should().Be(cursor);
        job.Produced.Should().Be(produced);
    }

    [Fact]
    public void ResetProgress_ShouldResetCursorAndProduced()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test" };
        job.UpdateProgress(10, "some progress");

        // Act
        job.ResetProgress();

        // Assert
        job.Cursor.Should().Be(0);
        job.Produced.Should().Be(string.Empty);
    }
}

