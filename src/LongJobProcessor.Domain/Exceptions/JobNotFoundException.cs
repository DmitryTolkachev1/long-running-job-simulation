namespace LongJobProcessor.Domain.Exceptions;

public sealed class JobNotFoundException : Exception
{
    public JobNotFoundException(Guid jobId)
        : base($"Job with ID {jobId} not found")
    {
    }
}
