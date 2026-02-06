using LongJobProcessor.Application.Common;
using MediatR;

namespace LongJobProcessor.Application.Jobs.Commands.CancelJob;

public sealed record CancelJobCommand(string UserId, Guid JobId) : IRequest<BaseResponse>;
