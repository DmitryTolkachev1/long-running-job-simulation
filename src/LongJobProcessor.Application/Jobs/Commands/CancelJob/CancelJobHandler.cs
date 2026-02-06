using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Application.Common;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongJobProcessor.Application.Jobs.Commands.CancelJob;

public sealed class CancelJobHandler : IRequestHandler<CancelJobCommand, BaseResponse>
{
    private readonly IJobRepository<Job> _jobRepository;
    private readonly ILogger<CancelJobHandler> _logger;

    public CancelJobHandler(IJobRepository<Job> jobRepository, ILogger<CancelJobHandler> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(CancelJobCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAsync(request.JobId, cancellationToken);

        if (job is null)
        {
            throw new JobNotFoundException(request.JobId);
        }

        if (job.UserId != request.UserId)
        {
            throw new UserMismatchException(request.UserId, request.JobId);
        }

        job.RequestCancel();

        await _jobRepository.UpdateAsync(job, cancellationToken);

        _logger.LogInformation($"Cancellation requested for job {request.JobId} by user {request.UserId}");

        return new();
    }
}
