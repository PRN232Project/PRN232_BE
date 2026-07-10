using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OnlineLearningPlatformApi.Application.IServices;

namespace OnlineLearningPlatformApi.Application.Services
{
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".avi", ".mkv", ".webm", ".mp3", ".wav" };

        public CloudinaryStorageService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Cloudinary credentials are not properly configured in appsettings.json.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadCourseImageAsync(string courseName, IFormFile file)
        {
            return await UploadFileToCloudinaryAsync(file, "courses", "image");
        }

        public async Task<string> UploadUserImageAsync(string userName, IFormFile file)
        {
            return await UploadFileToCloudinaryAsync(file, "users", "image");
        }

        public async Task<(string Url, int Type)> UploadLessonResourceAsync(Guid lessonId, string courseName, IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            int type = VideoExtensions.Contains(ext) ? 0 : 1; // 0 = Video/Audio, 1 = Reading/Document

            string resourceType = "raw";
            if (ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv" || ext == ".webm")
            {
                resourceType = "video";
            }
            else if (ext == ".mp3" || ext == ".wav" || ext == ".mpeg")
            {
                resourceType = "video"; // Cloudinary stores audio under video type
            }
            else if (ext == ".jpg" || ext == ".png" || ext == ".jpeg" || ext == ".gif")
            {
                resourceType = "image";
            }

            var url = await UploadFileToCloudinaryAsync(file, $"lessons/{lessonId}", resourceType);
            return (url, type);
        }

        public async Task<string> UploadQuestionSubmissionFileAsync(IFormFile file)
        {
            return await UploadFileToCloudinaryAsync(file, "submissions", "raw");
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileUrl))
                    return false;

                var uri = new Uri(fileUrl);
                var segments = uri.Segments;

                int uploadIndex = -1;
                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i].Equals("upload/", StringComparison.OrdinalIgnoreCase))
                    {
                        uploadIndex = i;
                        break;
                    }
                }

                if (uploadIndex == -1 || uploadIndex >= segments.Length - 1)
                    return false;

                int startIndex = uploadIndex + 1;
                if (segments[startIndex].StartsWith("v") && long.TryParse(segments[startIndex].Substring(1).TrimEnd('/'), out _))
                {
                    startIndex++;
                }

                var publicIdPath = string.Join("", segments.Skip(startIndex)).TrimEnd('/');
                var publicId = Path.ChangeExtension(publicIdPath, null);

                var isRaw = fileUrl.Contains("/raw/");
                var isVideo = fileUrl.Contains("/video/");

                var deletionParams = new DeletionParams(publicId)
                {
                    ResourceType = isRaw ? ResourceType.Raw : (isVideo ? ResourceType.Video : ResourceType.Image)
                };

                var result = await _cloudinary.DestroyAsync(deletionParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadFileToCloudinaryAsync(IFormFile file, string folder, string resourceType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            using (var stream = file.OpenReadStream())
            {
                var fileDesc = new FileDescription(file.FileName, stream);

                if (resourceType == "video")
                {
                    var uploadParams = new VideoUploadParams
                    {
                        File = fileDesc,
                        Folder = folder
                    };
                    var result = await _cloudinary.UploadAsync(uploadParams);
                    if (result.Error != null)
                        throw new Exception(result.Error.Message);
                    return result.SecureUrl.ToString();
                }
                else if (resourceType == "image")
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = fileDesc,
                        Folder = folder
                    };
                    var result = await _cloudinary.UploadAsync(uploadParams);
                    if (result.Error != null)
                        throw new Exception(result.Error.Message);
                    return result.SecureUrl.ToString();
                }
                else
                {
                    var uploadParams = new RawUploadParams
                    {
                        File = fileDesc,
                        Folder = folder
                    };
                    var result = await _cloudinary.UploadAsync(uploadParams);
                    if (result.Error != null)
                        throw new Exception(result.Error.Message);
                    return result.SecureUrl.ToString();
                }
            }
        }
    }
}
