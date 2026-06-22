using OnlineLearningPlatformApi.Application.Requests.LessonItem;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface ILessonItemService
    {
        Task<ApiResponse> GetLessonItemsByLessonAsync(Guid lessonId);
        Task<ApiResponse> GetLessonItemDetailAsync(Guid lessonItemId);
        Task<ApiResponse> CreateReadingItemAsync(CreateReadingItemRequest request);
        Task<ApiResponse> CreateVideoItemAsync(CreateVideoItemRequest request);
        Task<ApiResponse> CreateQuizItemAsync(CreateQuizItemRequest request);
        Task<ApiResponse> DeleteLessonItemAsync(Guid lessonItemId);
        Task<ApiResponse> CreateWritingItemAsync(CreateWritingItemRequest request);
        Task<ApiResponse> CreateSpeakingItemAsync(CreateSpeakingItemRequest request);
    }
}
