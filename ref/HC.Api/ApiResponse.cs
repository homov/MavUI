namespace HC.Api;

public class ApiResponse<T>: ApiResponseBase
{
    public T? Content
    {
        get => (T?)RawData;
        set => RawData = value;
    }
}

public class ApiResponseBase
{
    public object? RawData { get; set; }
    
    public HttpStatusCode StatusCode { get; init; }
    public bool IsSuccessStatusCode { get; init; }
    public string? ErrorMessage { get; init; }

    public void EnsureSuccessStatusCode()
    {
        if (!IsSuccessStatusCode)
            throw new ApiException(StatusCode, ErrorMessage);
    }
}

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string? message) : base(message)
    {
        StatusCode = statusCode;
    }
}
