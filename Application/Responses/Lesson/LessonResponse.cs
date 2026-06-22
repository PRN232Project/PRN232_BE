using OnlineLearningPlatformApi.Application.Responses.GradedItem;
using OnlineLearningPlatformApi.Application.Responses.LessonItem;

namespace OnlineLearningPlatformApi.Application.Responses.Lesson
{
    public class LessonResponse
    {
        public Guid LessonId { get; set; }
        public Guid ModuleId { get; set; }
        public string Title { get; set; }
        public string? Content { get; set; }

        public int OrderIndex { get; set; }
        public bool IsGraded { get; set; }
        public int EstimatedMinutes { get; set; }
        public List<GradedItemResponse>? GradedItems { get; set; }
        public List<LessonItemResponse> LessonItems { get; set; } = new();
    }
}