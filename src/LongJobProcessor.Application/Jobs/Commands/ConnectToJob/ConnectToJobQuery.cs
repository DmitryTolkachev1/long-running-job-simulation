using LongJobProcessor.Application.Common;
using MediatR;

namespace LongJobProcessor.Application.Jobs.Commands.ConnectToJob;

public sealed record ConnectToJobQuery(Guid JobId, string UserId, StreamWriter Writer) : IRequest<BaseResponse>;