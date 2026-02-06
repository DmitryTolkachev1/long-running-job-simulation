using LongJobProcessor.Application.Common;
using MediatR;

namespace LongJobProcessor.Application.Jobs.Queries.GetJobState;

public sealed record GetJobStateQuery(string UserId, Guid JobId) : IRequest<GetJobStateResult>;

public sealed class GetJobStateResult : BaseResponse
{
    public Guid JobId { get; set; }
    public string? JobStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
