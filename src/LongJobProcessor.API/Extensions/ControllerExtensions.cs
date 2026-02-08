using LongJobProcessor.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace LongJobProcessor.API.Extensions;

public static class ControllerExtensions
{
    public static ActionResult<T> ToActionResult<T>(this T response) where T : BaseResponse
    {
        return new ObjectResult(response)
        {
            StatusCode = response.StatusCode
        };
    }
}
