using HeatCeram.Api.Models;
using Refit;

namespace HeatController;

public class HeatCeramApiException: Exception
{
    public HeatCeramApiException(BaseResponse baseResponse, IApiResponse response): base(baseResponse.Description)
    {
        BaseResponse = baseResponse;
        Response = response;
    }

    public BaseResponse BaseResponse { get; }
    public IApiResponse Response { get; }
}
