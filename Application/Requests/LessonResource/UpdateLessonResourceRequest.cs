using Microsoft.AspNetCore.Http;

namespace OnlineLearningPlatformApi.Application.Requests.LessonResource
{
    public class UpdateLessonResourceRequest
    {
        public string? Title { get; set; }
        public IFormFile? File { get; set; }
    }
}
