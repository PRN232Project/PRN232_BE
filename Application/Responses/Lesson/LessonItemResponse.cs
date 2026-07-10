namespace OnlineLearningPlatformApi.Application.Responses.LessonItem
{
    public class LessonItemResponse
    {
        public Guid LessonItemId { get; set; }
        public int Type { get; set; }
        public int OrderIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ResourceUrl { get; set; }
        public string? Content { get; set; }
        public int? VideoSourceType { get; set; }
    }
}