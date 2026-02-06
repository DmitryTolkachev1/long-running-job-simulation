using LongJobProcessor.Domain.Enums;
using LongJobProcessor.Domain.StateMachine;

namespace LongJobProcessor.Domain.Entities.Jobs;

public abstract class Job
{
    public Guid Id { get; }
    public string UserId { get; }
    public JobStateMachine State { get; } = new();
    public JobStatus Status => State.Status;

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; protected set; }
    public DateTime? CompletedAt { get; protected set; }

    protected Job(string userId)
    {
        UserId = userId;
    }

    public void Enqueue() => State.Enqueue();
    public bool TryAcquire(string workerId, DateTimeOffset now, TimeSpan duration) => State.TryAcquireJob(workerId, now, duration);
    public void Start(string workerId)
    {
        State.Start(workerId);
        StartedAt ??= DateTime.UtcNow;
    }
    public void RequestCancel()
    {
        State.RequestCancel();
        CompletedAt ??= DateTime.UtcNow;
    }
    public void Complete(string workerId)
    {
        State.Complete(workerId);
        CompletedAt ??= DateTime.UtcNow;
    }
    public void Fail(string workerId)
    {
        State.Fail(workerId);
        CompletedAt ??= DateTime.UtcNow;
    }
}
