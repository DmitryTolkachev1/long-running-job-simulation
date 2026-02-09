namespace LongJobProcessor.Domain.Exceptions;

public sealed class UserMismatchException : Exception
{
    public UserMismatchException(string userId, Guid jobId)
        : base($"User {userId} is not authorized for this job {jobId}")
    {
    }
}
