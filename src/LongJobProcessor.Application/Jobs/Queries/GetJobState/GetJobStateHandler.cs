using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongJobProcessor.Application.Jobs.Queries.GetJobState;

public class GetJobStateHandler : IRequestHandler<GetJobStateQuery, GetJobStateResult>
{
    private readonly IJobRepository<Job> _jobRepository;
    private readonly ILogger<GetJobStateHandler> _logger;

    public GetJobStateHandler(IJobRepository<Job> jobRepository, ILogger<GetJobStateHandler> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<GetJobStateResult> Handle(GetJobStateQuery request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetAsync(request.JobId, cancellationToken);

        if (job is null)
        {
            throw new JobNotFoundException(request.JobId);
        }

        if (job.UserId != request.UserId)
        {
            throw new UserMismatchException(request.UserId, job.Id);
        }

        return new() { JobId = job.Id, JobStatus = job.Status.ToString(), CreatedAt = job.CreatedAt, StartedAt = job.StartedAt, CompletedAt = job.CompletedAt };
    }
}
