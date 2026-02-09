using System.Net;

namespace LongJobProcessor.Application.Common;

public class BaseResponse
{
    public BaseResponse()
    {
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
