using FluentAssertions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Infrastructure.Jobs;
using Xunit;

namespace LongJobProcessor.UnitTests.Infrastructure.Jobs;

public class JobFactoryTests
{
    private readonly JobFactory _factory;

    public JobFactoryTests()
    {
        _factory = new JobFactory();
    }

    [Fact]
    public void CreateJob_WithEncodeType_ShouldCreateInputEncodeJob()
    {
        // Arrange
        var userId = "user-1";
        var jobType = JobType.Encode;
        var jobData = new Dictionary<string, object> { { "Input", "test input" } };

        // Act
        var job = _factory.CreateJob(jobType, userId, jobData);

        // Assert
        job.Should().NotBeNull();
        job.Should().BeOfType<InputEncodeJob>();
        job.UserId.Should().Be(userId);
        var encodeJob = (InputEncodeJob)job;
        encodeJob.Input.Should().Be("test input");
    }

    [Fact]
    public void CreateJob_WithUnknownType_ShouldThrowNotImplementedException()
    {
        // Arrange
        var userId = "user-1";
        var jobType = JobType.Unknown;
        var jobData = new Dictionary<string, object>();

        // Act
        var act = () => _factory.CreateJob(jobType, userId, jobData);

        // Assert
        act.Should().Throw<NotImplementedException>();
    }

    [Fact]
    public void CreateJob_WithNullInput_ShouldThrowArgumentNullException()
    {
        // Arrange
        var userId = "user-1";
        var jobType = JobType.Encode;
        var jobData = new Dictionary<string, object> { { "Input", null! } };

        // Act
        var act = () => _factory.CreateJob(jobType, userId, jobData);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

