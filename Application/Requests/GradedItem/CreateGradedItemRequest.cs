using OnlineLearningPlatformApi.Application.Requests.Question;

namespace OnlineLearningPlatformApi.Application.Requests.GradedItem
{
    public class CreateGradedItemRequest
    {
        public Guid LessonItemId { get; set; }
        public int MaxScore { get; set; }
        public string? SubmissionGuidelines { get; set; }
        public List<CreateQuestionRequest>? Questions { get; set; }
    }
}
