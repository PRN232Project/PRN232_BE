using System.ComponentModel.DataAnnotations;

namespace OnlineLearningPlatformApi.Application.Requests.Lesson
{
    public class UpdateLessonRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int EstimatedMinutes { get; set; }
    }
}
