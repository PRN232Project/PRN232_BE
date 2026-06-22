using OnlineLearningPlatformApi.Application.Requests.LessonResource;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface ILessonResourceService
    {
        Task<ApiResponse> CreateLessonResourceAsync(CreateLessonResourceRequest request);
        Task<ApiResponse> GetResourcesByLessonItemAsync(Guid lessonItemId);
        Task<ApiResponse> UpdateLessonResourceAsync(Guid resourceId, UpdateLessonResourceRequest request);
        Task<ApiResponse> DeleteLessonResourceAsync(Guid resourceId);
    }
}
