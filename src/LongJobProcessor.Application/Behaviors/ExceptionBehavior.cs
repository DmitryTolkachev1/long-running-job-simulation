using LongJobProcessor.Application.Common;
using MediatR;

namespace LongJobProcessor.Application.Behaviors;

public class ExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TResponse : BaseResponse
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        BaseResponse response;

        try
        {
            response = await next();
        }
        catch (Exception ex)
        {
            response = await Task.FromResult(new BaseResponse(ex));
        }

        return (TResponse)response;
    }
}
