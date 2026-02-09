using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.API.Workers;

public sealed class JobCleanupWorker : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<JobCleanupWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);
    private const int _maxRetries = 3;

    public JobCleanupWorker(
        IJobQueue jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<JobCleanupWorker> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobCleanupWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("JobCleanupWorker is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JobCleanupWorker main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("JobCleanupWorker stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await HandleRetryingJobsAsync(now, cancellationToken);

        await HandleAbandonedJobsAsync(now, cancellationToken);

        await HandleStuckJobsAsync(now, cancellationToken);
    }

    private async Task HandleRetryingJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Job>>();

        var retryingJobs = await jobRepository.GetByStatusAsync(JobStatus.Retrying, cancellationToken);
        var jobs = retryingJobs.ToList();

        if (jobs.Count == 0)
        {
            return;
        }

        _logger.LogInformation($"Found {jobs.Count} jobs in retrying status, requeuing");

        foreach (var job in jobs)
        {
            try
            {
                var trackedJob = await jobRepository.GetAsync(job.Id, cancellationToken);
                if (trackedJob is null)
                {
                    continue;
                }

                if (trackedJob.Status != JobStatus.Retrying)
                {
                    continue;
                }

                trackedJob.State.Enqueue();

                await jobRepository.UpdateAsync(trackedJob, cancellationToken);

                await _jobQueue.EnqueueAsync(trackedJob.Id, cancellationToken);

                _logger.LogInformation($"Requeued job {trackedJob.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to requeue job {job.Id}");
            }
        }
    }

    private async Task HandleAbandonedJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Job>>();

        var abandonedJobs = await jobRepository.GetByStatusAsync(JobStatus.Abandoned, cancellationToken);
        var jobs = abandonedJobs.ToList();

        if (jobs.Count == 0)
        {
            return;
        }

        _logger.LogInformation($"Found {jobs.Count} jobs in abandoned status, checking ownership");

        foreach (var job in jobs)
        {
            try
            {
                var trackedJob = await jobRepository.GetAsync(job.Id, cancellationToken);
                if (trackedJob is null)
                {
                    continue;
                }

                if (trackedJob.Status != JobStatus.Abandoned)
                {
                    continue;
                }

                if (trackedJob.State.RetryCount <= _maxRetries)
                {
                    trackedJob.State.Retry();

                    await jobRepository.UpdateAsync(trackedJob, cancellationToken);

                    await _jobQueue.EnqueueAsync(trackedJob.Id, cancellationToken);

                    _logger.LogInformation($"Requeued job {trackedJob.Id}");
                }
                else
                {
                    trackedJob.State.Fail(trackedJob.State.JobOwner);
                    await jobRepository.UpdateAsync(trackedJob, cancellationToken);

                    _logger.LogInformation($"Moved job {trackedJob.Id} to Failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process abandoned job {job.Id}");
            }
        }
    }

    private async Task HandleStuckJobsAsync(DateTime now, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Job>>();

        var cancellingJobs = await jobRepository.GetByStatusAsync(JobStatus.Cancelling, cancellationToken);
        var jobs = cancellingJobs.ToList();

        if (jobs.Count == 0)
        {
            return;
        }

        _logger.LogInformation($"Found {jobs.Count} jobs in cancelling status, checkibng for stuck jobs");

        foreach (var job in jobs)
        {
            try
            {
                var trackedJob = await jobRepository.GetAsync(job.Id, cancellationToken);
                if (trackedJob is null)
                {
                    continue;
                }

                if (trackedJob.Status != JobStatus.Cancelling)
                {
                    continue;
                }

                var isStuck = trackedJob.State.JobOwner is null ||
                    (trackedJob.State.TakenUntil.HasValue &&
                    now > trackedJob.State.TakenUntil.Value.Add(_lockDuration));

                if (isStuck)
                {
                    trackedJob.State.CancelStuck(now, _lockDuration);
                    await jobRepository.UpdateAsync(trackedJob, cancellationToken);

                    _logger.LogInformation($"Cancelled stuck job {job.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to requeue job {job.Id}");
            }
        }
    }
}
