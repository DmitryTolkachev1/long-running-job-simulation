using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Domain.Exceptions;

public sealed class InvalidStateTransitionException : Exception
{
    public InvalidStateTransitionException(JobStatus status) 
        : base($"Invalid transition from {status}")
    {
    }
}
