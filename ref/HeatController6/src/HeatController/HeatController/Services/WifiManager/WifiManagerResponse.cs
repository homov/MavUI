namespace HeatController.Services.WifiManager;

public class WifiManagerResponse<T>
{
    public T Data { get; set; }
    public WifiErrorCodes ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public static WifiManagerResponse<T> SuccessResponse(T? data, string errorMessage)
    {
        return new WifiManagerResponse<T>
        {
            Data = data,
            ErrorCode = WifiErrorCodes.Success,
            ErrorMessage = errorMessage
        };
    }

    public static WifiManagerResponse<T> ErrorResponse(WifiErrorCodes errorCode, string errorMessage)
    {
        return new WifiManagerResponse<T>
        {
            Data = default,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

}
