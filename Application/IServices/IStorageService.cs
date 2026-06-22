using Microsoft.AspNetCore.Http;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IStorageService
    {
        Task<string> UploadCourseImageAsync(string courseName, IFormFile file);
        Task<string> UploadUserImageAsync(string userName, IFormFile file);
        Task<(string Url, int Type)> UploadLessonResourceAsync(Guid lessonId, string courseName, IFormFile file);
        Task<string> UploadQuestionSubmissionFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}