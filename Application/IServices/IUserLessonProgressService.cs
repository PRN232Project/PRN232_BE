using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Requests.UserLessonProgress;   
namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IUserLessonProgressService
    {
        Task<ApiResponse> StartOrUpdateProgressAsync(UpdateUserLessonProgressRequest request);
        Task<ApiResponse> MarkLessonCompletedAsync(Guid lessonId);
        Task<ApiResponse> GetLessonProgressAsync(Guid lessonId);
        Task<ApiResponse> GetLessonProgressByUserAsync(Guid userId);
    }
}
