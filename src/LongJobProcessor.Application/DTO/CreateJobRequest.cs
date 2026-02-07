using LongJobProcessor.Domain.Enums;

namespace LongJobProcessor.Application.DTO;

public sealed class CreateJobRequest
{
    public required JobType JobType { get; set; }
    public required Dictionary<string, object> JobData { get; set; }
}
