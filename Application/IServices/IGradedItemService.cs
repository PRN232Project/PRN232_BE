using OnlineLearningPlatformApi.Application.Requests.GradedItem;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IGradedItemService
    {
        Task<ApiResponse> SubmitQuizAsync(SubmitQuizRequest request);
    }
}
