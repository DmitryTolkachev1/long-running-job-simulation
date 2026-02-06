using LongJobProcessor.Application.Abstractions;
using LongJobProcessor.Domain.Entities.Jobs;
using LongJobProcessor.Domain.Entities.Jobs.InputEncode;
using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Infrastructure.Jobs;

public class JobFactory : IJobFactory
{
    public Job CreateJob(JobType type, string userId, Dictionary<string, object> jobPayload)
    {
        return type switch
        {
            JobType.Encode => new InputEncodeJob(userId) { Input = jobPayload["Input"].ToString() ?? throw new ArgumentNullException("Input") },
            _ => throw new NotImplementedException(),
        };
    }
}
