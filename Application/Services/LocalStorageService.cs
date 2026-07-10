using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using OnlineLearningPlatformApi.Application.IServices;

namespace OnlineLearningPlatformApi.Application.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Các extension coi là video khi upload lesson resource
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };

        public LocalStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> UploadCourseImageAsync(string courseName, IFormFile file)
        {
            return await SaveFileAsync(file, "courses");
        }

        public async Task<string> UploadUserImageAsync(string userName, IFormFile file)
        {
            return await SaveFileAsync(file, "users");
        }

        public async Task<(string Url, int Type)> UploadLessonResourceAsync(Guid lessonId, string courseName, IFormFile file)
        {
            var url = await SaveFileAsync(file, $"lessons/{lessonId}");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            int type = VideoExtensions.Contains(ext) ? 0 : 1; // 0 = Video, 1 = Reading/Document

            return (url, type);
        }

        public async Task<string> UploadQuestionSubmissionFileAsync(IFormFile file)
        {
            return await SaveFileAsync(file, "submissions");
        }

        public Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl))
                    return Task.FromResult(false);

                // fileUrl dạng: https://host/uploads/courses/abc.jpg
                var uri = new Uri(fileUrl);
                var relativePath = uri.AbsolutePath.TrimStart('/'); // uploads/courses/abc.jpg

                var physicalPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(uploadsRoot); // tự tạo thư mục nếu chưa có

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var physicalPath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : string.Empty;

            var relativeUrl = $"/uploads/{subFolder}/{fileName}".Replace("\\", "/");

            return $"{baseUrl}{relativeUrl}";
        }
    }
}