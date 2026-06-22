using OnlineLearningPlatformApi.Application.Requests.Lesson;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface ILessonService
    {
        Task<ApiResponse> CreateNewLessonForModuleAsync(CreateNewLessonForModuleRequest request);
        Task<ApiResponse> UpdateLessonAsync(Guid lessonId, UpdateLessonRequest request);
        Task<ApiResponse> DeleteLessonAsync(Guid lessonId);

        Task<ApiResponse> GetLessonDetailAsync(Guid lessonId);
        Task<ApiResponse> GetLessonsByModuleAsync(Guid moduleId);
    }
}
