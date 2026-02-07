using System.Net;

namespace LongJobProcessor.Application.Common;

public class BaseResponse
{
    public BaseResponse()
    {
    }

    public BaseResponse(Exception ex)
    {
        this.StatusCode = (int)HttpStatusCode.InternalServerError;
        this.Message = "A System Error occured";
        this.Success = false;
        if (ex.InnerException?.Message != null)
        {
            this.AddError(ex.InnerException.Message);
        }

        if (ex.InnerException?.InnerException?.Message != null)
        {
            this.AddError(ex.InnerException.InnerException.Message);
        }

        if (ex.Message != null)
        {
            this.AddError(ex.Message);
        }
    }

    public bool Success { get; set; } = true;

    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    public List<string>? Errors { get; set; }

    public string? Message { get; set; }

    public void AddError(string description)
    {
        if (this.Errors == null)
        {
            this.Errors = new List<string>();
        }

        this.Errors.Add(description);
    }
}
