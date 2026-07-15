using System;

namespace OnlineLearningPlatformApi.Application.Requests.Practice
{
    public class SubmitPracticeAttemptRequest
    {
        public Guid LessonItemId { get; set; }
        public string SubmittedText { get; set; } = null!;
        public decimal Score { get; set; }
        public string Feedback { get; set; } = null!;
        public bool IsPassed { get; set; }
    }
}
