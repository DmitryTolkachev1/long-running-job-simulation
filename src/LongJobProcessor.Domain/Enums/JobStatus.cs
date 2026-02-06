namespace LongJobProcessor.Domain.Enums;

public enum JobStatus
{
    Created,
    Queued,
    Taken,
    Running,
    Cancelling,
    Cancelled,
    Completed,
    Failed,
    Abandoned,
    Retrying,
}
