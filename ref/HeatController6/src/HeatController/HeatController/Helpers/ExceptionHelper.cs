using System.Net.Sockets;

namespace HeatController;

public static class ExceptionHelper
{ 
    public static Dictionary<string,string> ErrorMessages = new Dictionary<string,string>();

    public static string GetUserMessage(Exception exception)
    {
        if (exception is HeatCeramApiException apiException)
        {
            var url = string.Empty;
            if (apiException.Response.RequestMessage != null)
                url = apiException.Response.RequestMessage.RequestUri.AbsoluteUri;

            var content = apiException.BaseResponse;
            var contentDescription = string.Empty;
            if (content != null && ErrorMessages.ContainsKey(content.ErrorCode))
                return ErrorMessages[content.ErrorCode];

            if (content != null && content.Description != null)
                contentDescription = content.Description.ToLower();
            if (contentDescription.Contains("wrong data") && url.Contains("user_authorization"))
                return Resources.InvalidEmailOrPassword;
            else if (contentDescription.Contains("name already used"))
                return Resources.NameAlreadyUsedText;
        }
        else if (exception is TaskCanceledException && exception.InnerException != null)
            return GetUserMessage(exception.InnerException);
        else if (exception is SocketException)
        {
            return Resources.ConnectionErrorText;
        }
        else if (exception is System.IO.IOException)
        {
            return Resources.ConnectionErrorText;
        }

        return exception.Message;
    }
}
