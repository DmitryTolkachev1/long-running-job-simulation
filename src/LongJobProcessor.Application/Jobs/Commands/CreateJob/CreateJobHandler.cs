using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LongJobProcessor.Application.Jobs.Commands.CreateJob;

public sealed class CreateJobHandler : IRequestHandler<CreateJobCommand, CreateJobResult>
{
    private readonly IJobFactory _jobFactory;
    private readonly IJobRepository<Job> _jobRepository;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<CreateJobHandler> _logger;

    public CreateJobHandler(IJobFactory jobFactory, IJobRepository<Job> jobRepository, IJobQueue jobQueue, ILogger<CreateJobHandler> logger)
    {
        _jobFactory = jobFactory;
        _jobRepository = jobRepository;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    public async Task<CreateJobResult> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var job = _jobFactory.CreateJob(request.JobType, request.UserId, request.JobData);

        job.Enqueue();

        await _jobRepository.AddAsync(job, cancellationToken);

        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);

        _logger.LogInformation($"Created job {job.Id} of type {request.JobType} for user {request.UserId}");

        return new(){ JobId = job.Id };
    }
}
