using Refit;
using System.Net;

namespace HeatController;

public class HttpApiException: Exception
{

    public HttpApiException(HttpStatusCode httpStatusCode, ApiException exception)
        :base($"Http error. Server return code {httpStatusCode}")
    {
        HttpStatusCode = httpStatusCode;
        Exception = exception;
    }

    public HttpStatusCode HttpStatusCode { get; }
    public ApiException Exception { get; }
}
