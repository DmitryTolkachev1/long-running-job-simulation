using LongJobProcessor.API.Extensions;
using LongJobProcessor.Application.Common;
using LongJobProcessor.Application.DTO;
using LongJobProcessor.Application.Jobs.Commands.CancelJob;
using LongJobProcessor.Application.Jobs.Commands.ConnectToJob;
using LongJobProcessor.Application.Jobs.Commands.CreateJob;
using LongJobProcessor.Application.Jobs.Queries.GetJobState;
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

        var result = await _mediator.Send(query, cancellationToken);

        return result.ToActionResult();
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

        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<CreateJobResult>> CreateJob(CreateJobRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new CreateJobCommand(userId, request.JobType, request.JobData);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Route("{jobId}/cancel")]
    public async Task<ActionResult<BaseResponse>> CancelJob([FromRoute] Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var command = new CancelJobCommand(userId, jobId);

        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    private string GetUserId()
    {
        if (User?.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
        {
            return User.Identity.Name;
        }

        return Request.Headers["X-User-Id"].FirstOrDefault() ?? "anonymous";
    }
}