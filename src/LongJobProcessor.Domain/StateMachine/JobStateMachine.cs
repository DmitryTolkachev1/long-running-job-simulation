using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Domain.Exceptions;

namespace LongJobProcessor.Domain.StateMachine;

public sealed class JobStateMachine
{
    private const int _maxRetries = 3;

    public JobStatus Status { get; private set; }
    public string? JobOwner { get; private set; }
    public DateTime? TakenUntil { get; private set; }
    public int RetryCount { get; private set; }

    public JobStateMachine()
        : this(JobStatus.Created)
    {
    }

    public JobStateMachine(JobStatus initial = JobStatus.Created)
    {
        Status = initial;
    }

    public void Enqueue()
    {
        Ensure(JobStatus.Created);
        Status = JobStatus.Queued;
    }

    public bool TryAcquireJob(string workerId, DateTime now, TimeSpan duration) 
    {
        if (Status != JobStatus.Queued && Status != JobStatus.Retrying) 
        {
            return false;
        }

        CheckOwnershipExpired(now, _maxRetries);

        JobOwner = workerId;
        TakenUntil = now.Add(duration);
        Status = JobStatus.Taken;
        return true;
    }

    public void Start(string workerId) 
    {
        Ensure(JobStatus.Taken);
        EnsureOwner(workerId);
        Status = JobStatus.Running;
    }

    public void Heartbeat(string workerId, DateTime now, TimeSpan duration)
    {
        Ensure(JobStatus.Taken, JobStatus.Running, JobStatus.Cancelling);
        EnsureOwner(workerId);
        TakenUntil = now.Add(duration);
    }

    public void RequestCancel() 
    {
        switch (Status)
        {
            case JobStatus.Queued:
                Status = JobStatus.Cancelled;
                break;
            case JobStatus.Taken:
            case JobStatus.Running:
                Status = JobStatus.Cancelling;
                break;
        }

        ClearOwnership();
    }

    public void Retry()
    {
        Ensure(JobStatus.Abandoned);
        Status = JobStatus.Retrying;
    }

    public void Complete(string workerId)
    {
        Ensure(JobStatus.Running, JobStatus.Cancelling);
        EnsureOwner(workerId);
        Status = JobStatus.Completed;
        ClearOwnership();
    }

    public void CancelByWorker(string workerId)
    {
        Ensure(JobStatus.Cancelling);
        EnsureOwner(workerId);
        Status = JobStatus.Cancelled;
        ClearOwnership();
    }

    public void Fail(string workerId)
    {
        Ensure(JobStatus.Running, JobStatus.Cancelling, JobStatus.Taken);
        EnsureOwner(workerId);
        Status = JobStatus.Failed;
        ClearOwnership();
    }

    public void CheckOwnershipExpired(DateTime now, int maxRetries)
    {
        if (TakenUntil is null)
        {
            return;
        }

        if (now <= TakenUntil.Value)
        {
            return;
        }

        if (Status is JobStatus.Taken or JobStatus.Running or JobStatus.Cancelling)
        {
            Status = JobStatus.Abandoned;
            ClearOwnership();
            RetryCount++;

            if (RetryCount <= maxRetries) 
            {
                Status = JobStatus.Retrying;
            }
            else
            {
                Status = JobStatus.Failed;
            }
        }
    }

    public void CancelStuck(DateTime now, TimeSpan duration)
    {
        if (Status != JobStatus.Cancelling)
        {
            throw new InvalidStateTransitionException(Status);
        }

        if (JobOwner is null || (TakenUntil.HasValue && now > TakenUntil.Value.Add(duration)))
        {
            Status = JobStatus.Cancelled;
            ClearOwnership();
        }
        else
        {
            throw new InvalidOperationException("Cannot cancel job that is still owned by a worker");
        }
    }

    private void Ensure(params JobStatus[] allowed) 
    {
        foreach (var status in allowed) 
        {
            if (Status == status) 
            {
                return;
            }
        }

        throw new InvalidStateTransitionException(Status);
    }

    private void EnsureOwner(string workerId)
    {
        if (JobOwner != workerId)
        {
            throw new OwnerMismatchException(workerId);
        }
    }

    private void ClearOwnership()
    {
        JobOwner = null;
        TakenUntil = null;
    }
}
