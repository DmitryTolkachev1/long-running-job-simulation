using LongJobProcessor.Application.Common;
using MediatR;
using System.Net;

namespace LongJobProcessor.Application.Behaviors;

public class ExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TResponse : BaseResponse, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var response = new TResponse();
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = "A System Error occured";
            response.Success = false;
            if (ex.InnerException?.Message != null)
            {
                response.AddError(ex.InnerException.Message);
            }

            if (ex.InnerException?.InnerException?.Message != null)
            {
                response.AddError(ex.InnerException.InnerException.Message);
            }

            if (ex.Message != null)
            {
                response.AddError(ex.Message);
            }

            return response;
        }
    }
}
