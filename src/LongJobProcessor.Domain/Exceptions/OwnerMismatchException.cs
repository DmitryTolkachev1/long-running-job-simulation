namespace LongJobProcessor.Domain.Exceptions;

public sealed class OwnerMismatchException : Exception
{
    public OwnerMismatchException(string workerId)
        : base($"Worker {workerId} does not own the job")
    {
    }
}
