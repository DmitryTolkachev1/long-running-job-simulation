using FluentAssertions;
using LongJobProcessor.Application.Workers;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using System.Text;
using Xunit;

namespace LongJobProcessor.UnitTests.Application.Workers;

public class InputEncodeJobExecutorTests
{
    private readonly InputEncodeJobExecutor _executor;

    public InputEncodeJobExecutorTests()
    {
        _executor = new InputEncodeJobExecutor();
    }

    [Fact]
    public void JobType_ShouldReturnEncode()
    {
        // Assert
        _executor.JobType.Should().Be(JobType.Encode);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessJobAndCallProgressCallback()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "abc" };
        var progressUpdates = new List<object>();
        var progressCallback = new Func<object, Task>(data =>
        {
            progressUpdates.Add(data);
            return Task.CompletedTask;
        });
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

        // Act
        await _executor.ExecuteAsync(job, progressCallback, cancellationToken);

        // Assert
        progressUpdates.Should().NotBeEmpty();
        job.Produced.Should().NotBeNullOrEmpty();
        job.Cursor.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "test input that will take time" };
        var progressCallback = new Func<object, Task>(_ => Task.CompletedTask);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var cancellationToken = cts.Token;

        // Act
        var act = async () => await _executor.ExecuteAsync(job, progressCallback, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithResume_ShouldContinueFromCursor()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "abc" };
        var expectedEncoded = BuildExpectedEncoded("abc");
        job.UpdateProgress(2, expectedEncoded.Substring(0, 2));
        
        var progressUpdates = new List<object>();
        var progressCallback = new Func<object, Task>(data =>
        {
            progressUpdates.Add(data);
            return Task.CompletedTask;
        });
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

        // Act
        await _executor.ExecuteAsync(job, progressCallback, cancellationToken);

        // Assert
        job.Produced.Should().Be(expectedEncoded);
        job.Cursor.Should().Be(expectedEncoded.Length);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidProgress_ShouldReset()
    {
        // Arrange
        var job = new InputEncodeJob("user-1") { Input = "abc" };
        job.UpdateProgress(10, "invalid progress");
        
        var progressUpdates = new List<object>();
        var progressCallback = new Func<object, Task>(data =>
        {
            progressUpdates.Add(data);
            return Task.CompletedTask;
        });
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token;

        // Act
        await _executor.ExecuteAsync(job, progressCallback, cancellationToken);

        // Assert
        job.Produced.Should().NotBeNullOrEmpty();
        job.Cursor.Should().BeGreaterThan(0);
    }

    private static string BuildExpectedEncoded(string input)
    {
        var counts = new Dictionary<char, int>();
        foreach (var character in input)
        {
            if (character == ' ')
            {
                continue;
            }

            if (!counts.TryAdd(character, 1))
            {
                counts[character]++;
            }
        }

        var sorted = counts.Keys.OrderBy(c => c).ToList();

        var prefix = new StringBuilder();
        foreach (var character in sorted)
        {
            prefix.Append(counts[character]);
            prefix.Append(character);
        }

        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(input));

        return prefix.Append('/').Append(base64).ToString();
    }
}

