using OnlineLearningPlatformApi.Application.Requests.AnswerOption;

namespace OnlineLearningPlatformApi.Application.Requests.Question
{
    public class UpdateQuestionRequest
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; }
        public decimal Points { get; set; }
        public string? Explanation { get; set; }
        public int Type { get; set; }

        public List<CreateAnswerOptionRequest>? AnswerOptions { get; set; }
    }
}