using LongJobProcessor.Application.Common;
using LongJobProcessor.Application.Jobs.Commands.CancelJob;
using LongJobProcessor.Application.Jobs.Commands.ConnectToJob;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Application.Jobs.Queries.GetJobState;
using LongJobProcessor.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LongJobProcessor.API.Controllers;

[Route("api/jobs")]
[ApiController]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Route("{jobId}/state")]
    public async Task<ActionResult<GetJobStateResult>> GetJobState([FromRoute] Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var query = new GetJobStateQuery(userId, jobId);

        return await _mediator.Send(query, cancellationToken);
    }

    [HttpGet]
    [Route("{jobId}/connection")]
    public async Task<ActionResult<BaseResponse>> SubscribeToJobProgress([FromRoute] Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var writer = new StreamWriter(Response.Body, leaveOpen: true) { AutoFlush = false };
        var query = new ConnectToJobQuery(jobId, userId, writer);
        return await _mediator.Send(query, cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<CreateJobResult>> CreateJob(JobType jobType, Dictionary<string, object> payload, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new CreateJobCommand(userId, jobType, payload);

        return await _mediator.Send(command, cancellationToken);
    }

    [HttpPost]
    [Route("{jobId}/cancel")]
    public async Task<ActionResult<BaseResponse>> CancelJob([FromRoute] Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new CancelJobCommand(userId, jobId);

        return await _mediator.Send(command, cancellationToken);
    }

    private string GetUserId()
    {
        return Request.Headers["X-User-Id"].FirstOrDefault()
            ?? User?.Identity?.Name
            ?? "anonymous";
    }
}