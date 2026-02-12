using FluentAssertions;
using LongJobProcessor.API.Controllers;
using LongJobProcessor.Application.Common;
using LongJobProcessor.Application.DTO;
using LongJobProcessor.Application.Jobs.Commands.CancelJob;
using LongJobProcessor.Application.Jobs.Commands.ConnectToJob;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Application.Jobs.Queries.GetJobState;
using LongJobProcessor.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading;
using Xunit;

namespace LongJobProcessor.UnitTests.API.Controllers;

public class JobsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new JobsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task CreateJob_ShouldReturnCreateJobResult()
    {
        // Arrange
        var userId = "user-1";
        var request = new CreateJobRequest
        {
            JobType = JobType.Encode,
            JobData = new Dictionary<string, object> { { "Input", "test" } }
        };
        var expectedResult = new CreateJobResult { JobId = Guid.NewGuid() };
        var cancellationToken = CancellationToken.None;

        SetupControllerContext(userId);
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreateJobCommand>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateJob(request, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Result.As<ObjectResult>().Value.Should().Be(expectedResult);
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateJobCommand>(c => c.UserId == userId && c.JobType == request.JobType),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetJobState_ShouldReturnJobState()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var expectedResult = new GetJobStateResult
        {
            JobId = jobId,
            JobStatus = "Queued",
            CreatedAt = DateTime.UtcNow
        };
        var cancellationToken = CancellationToken.None;

        SetupControllerContext(userId);
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetJobStateQuery>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetJobState(jobId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Result.As<ObjectResult>().Value.Should().Be(expectedResult);
        _mediatorMock.Verify(x => x.Send(
            It.Is<GetJobStateQuery>(q => q.UserId == userId && q.JobId == jobId),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CancelJob_ShouldReturnBaseResponse()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var expectedResult = new BaseResponse { Success = true };
        var cancellationToken = CancellationToken.None;

        SetupControllerContext(userId);
        _mediatorMock.Setup(x => x.Send(It.IsAny<CancelJobCommand>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CancelJob(jobId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Result.As<ObjectResult>().Value.Should().Be(expectedResult);
        _mediatorMock.Verify(x => x.Send(
            It.Is<CancelJobCommand>(c => c.UserId == userId && c.JobId == jobId),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SubscribeToJobProgress_ShouldReturnBaseResponse()
    {
        // Arrange
        var userId = "user-1";
        var jobId = Guid.NewGuid();
        var expectedResult = new BaseResponse { Success = true };
        var cancellationToken = CancellationToken.None;
        var response = new Mock<HttpResponse>();
        var responseBody = new MemoryStream();
        var responseHeaders = new HeaderDictionary();

        response.Setup(x => x.Body).Returns(responseBody);
        response.Setup(x => x.Headers).Returns(responseHeaders);
        response.Setup(x => x.ContentType).Returns("text/event-stream");

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.Response).Returns(response.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object
        };

        SetupControllerContext(userId);
        _mediatorMock.Setup(x => x.Send(It.IsAny<ConnectToJobQuery>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SubscribeToJobProgress(jobId, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _mediatorMock.Verify(x => x.Send(
            It.Is<ConnectToJobQuery>(q => q.UserId == userId && q.JobId == jobId),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetUserId_FromHeader_ShouldReturnHeaderValue()
    {
        // Arrange
        var userId = "user-from-header";
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var expectedResult = new CreateJobResult { JobId = Guid.NewGuid() };
        var cancellationToken = CancellationToken.None;
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreateJobCommand>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateJob(
            new CreateJobRequest { JobType = JobType.Encode, JobData = new Dictionary<string, object>() },
            CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateJobCommand>(c => c.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserId_FromUserIdentity_ShouldReturnIdentityName()
    {
        // Arrange
        var userId = "user-from-identity";
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, userId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var expectedResult = new CreateJobResult { JobId = Guid.NewGuid() };
        var cancellationToken = CancellationToken.None;
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreateJobCommand>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateJob(
            new CreateJobRequest { JobType = JobType.Encode, JobData = new Dictionary<string, object>() },
            CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateJobCommand>(c => c.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserId_WhenNoHeaderOrIdentity_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        var expectedResult = new CreateJobResult { JobId = Guid.NewGuid() };
        var cancellationToken = CancellationToken.None;
        _mediatorMock.Setup(x => x.Send(It.IsAny<CreateJobCommand>(), cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateJob(
            new CreateJobRequest { JobType = JobType.Encode, JobData = new Dictionary<string, object>() },
            CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<CreateJobCommand>(c => c.UserId == "anonymous"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupControllerContext(string userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }
}

