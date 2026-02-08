using LongJobProcessor.Application.Common;
using LongJobProcessor.Domain.Enums;
using MediatR;

namespace LongJobProcessor.Application.Jobs.Commands.CreateJob;

public sealed record CreateJobCommand(string UserId, JobType JobType, Dictionary<string, object> JobData) : IRequest<CreateJobResult>;

public sealed class CreateJobResult : BaseResponse
{
    public Guid JobId { get; set; }
}
