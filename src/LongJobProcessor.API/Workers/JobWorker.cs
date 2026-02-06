using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Application.Workers;

public sealed class JobWorker : BackgroundService
{
    private readonly IJobQueue _jobQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEnumerable<IJobExecutor> _jobExecutors;
    private readonly IProgressNotifier _progressNotifier;
    private readonly ILogger<JobWorker> _logger;
    private readonly string _workerId;
    private readonly TimeSpan _heartBeatInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);

    public JobWorker(
        IJobQueue jobQueue,
        IServiceScopeFactory serviceScopeFactory,
        IEnumerable<IJobExecutor> jobExecutors,
        IProgressNotifier progressNotifier,
        ILogger<JobWorker> logger)
    {
        _jobQueue = jobQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _jobExecutors = jobExecutors;
        _progressNotifier = progressNotifier;
        _logger = logger;
        _workerId = Environment.MachineName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"JobWorker {_workerId} started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _jobQueue.DequeueAsync(stoppingToken);
                _ = Task.Run(() => ProcessJobAsync(jobId, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"JobWorker {_workerId} is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JobWorker {_workerId} main loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation($"JobWorker {_workerId} stopped");
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        Job? job = null;
        using var scope = _serviceScopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Job>>();

        try
        {
            job = await jobRepository.GetAsync(jobId, cancellationToken);
            if (job is null)
            {
                _logger.LogWarning($"Job {jobId} not found, skipping");
                return;
            }

            if (job.Status != JobStatus.Queued && job.Status != JobStatus.Retrying)
            {
                _logger.LogInformation($"Job {jobId} is in status {job.Status}, skipping");
                return;
            }

            var now = DateTime.UtcNow;
            if (!job.State.TryAcquireJob(_workerId, now, _lockDuration))
            {
                _logger.LogInformation($"Failed to acquire Job {jobId}, status: {job.Status}");
                return;
            }

            await jobRepository.UpdateAsync(job, cancellationToken);
            _logger.LogInformation($"Acquired job {jobId}");


            job.Start(_workerId);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await _progressNotifier.NotifyStatusAsync(job.UserId, jobId, job.Status.ToString(), cancellationToken);

            var jobType = GetJobType(job);
            var executor = _jobExecutors.FirstOrDefault(e => e.JobType == jobType);

            if (executor is null)
            {
                throw new InvalidOperationException($"No executor found for job type: {jobType}");
            }

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeatTask = StartHeartbeatAsync(job, heartbeatCts.Token);

            try
            {
                using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var cancellationCheckTask = Task.Run(async () =>
                {
                    while (!jobCts.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1), jobCts.Token);

                            var reloadedJob = await jobRepository.GetAsync(jobId, jobCts.Token);
                            if (reloadedJob?.Status == JobStatus.Cancelling)
                            {
                                jobCts.Cancel();
                                break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }, jobCts.Token);

                await executor.ExecuteAsync(
                    job,
                    async (data) =>
                    {
                        if (jobCts.IsCancellationRequested)
                        {
                            jobCts.Token.ThrowIfCancellationRequested();
                        }
                        await _progressNotifier.NotifyProgressAsync(job.UserId, jobId, data, jobCts.Token);
                    },
                    jobCts.Token);

                jobCts.Cancel();
                try { await cancellationCheckTask; } catch { }

                heartbeatCts.Cancel();
                try { await heartbeatTask; } catch { }

                job.Complete(_workerId);

                await jobRepository.UpdateAsync(job, cancellationToken);
                await _progressNotifier.NotifyStatusAsync(job.UserId, jobId, job.Status.ToString(), cancellationToken);

                _logger.LogInformation($"Completed job {jobId}");
            }
            catch (OperationCanceledException)
            {
                heartbeatCts.Cancel();
                try { await heartbeatTask; } catch { }

                job = await jobRepository.GetAsync(jobId, cancellationToken);
                if (job is not null && job.Status == JobStatus.Cancelling)
                {
                    job.State.CancelByWorker(_workerId);
                    await jobRepository.UpdateAsync(job, cancellationToken);
                    await _progressNotifier.NotifyStatusAsync(job.UserId, jobId, job.Status.ToString(), cancellationToken);

                    _logger.LogInformation($"Cancelled job {jobId}");
                }
            }
            catch (Exception ex)
            {
                heartbeatCts.Cancel();
                try { await heartbeatTask; } catch { }

                _logger.LogError(ex, $"Error processing job {jobId}");

                job.Fail(_workerId);
                await jobRepository.UpdateAsync(job, cancellationToken);
                await _progressNotifier.NotifyStatusAsync(job.UserId, jobId, job.Status.ToString(), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing job {jobId}");
            if (job is not null)
            {
                try
                {
                    job.Fail(_workerId);
                    await jobRepository.UpdateAsync(job, cancellationToken);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, $"Failed to update job {jobId} status after error");
                }
            }
        }
    }

    private async Task StartHeartbeatAsync(Job job, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Job>>();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_heartBeatInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var now = DateTime.UtcNow;
                job.State.Heartbeat(_workerId, now, _lockDuration);
                await jobRepository.UpdateAsync(job, cancellationToken);
                _logger.LogTrace($"Heartbeat for job {job.Id}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error in heartbeat in job {job.Id}");
        }
    }

    private static JobType GetJobType(Job job)
    {
        return job switch
        {
            InputEncodeJob => JobType.Encode,
            _ => throw new InvalidOperationException($"Unknown job type: {job.GetType().Name}")
        };
    }
}
