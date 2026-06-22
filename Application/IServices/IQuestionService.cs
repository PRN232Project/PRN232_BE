using OnlineLearningPlatformApi.Application.Requests.Question;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IQuestionService
    {
        Task<ApiResponse> CreateQuestionAsync(CreateQuestionRequest request);
    }
}
