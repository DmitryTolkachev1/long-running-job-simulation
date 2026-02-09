using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Application.Abstractions;

public interface IJobFactory
{
    Job CreateJob(JobType type, string userId, Dictionary<string, object> jobPayload);
}
