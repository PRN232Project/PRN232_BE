namespace OnlineLearningPlatformApi.Application.Responses.Course
{
    public class StudentLearningDetailResponse
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal ProgressPercent { get; set; }
        public List<StudentLearningModuleResponse> Modules { get; set; } = new();
    }

    public class StudentLearningModuleResponse
    {
        public Guid ModuleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<StudentLearningLessonResponse> Lessons { get; set; } = new();
    }

    public class StudentLearningLessonResponse
    {
        public Guid LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public int EstimatedMinutes { get; set; }
        public bool IsCompleted { get; set; }
        public List<StudentLearningMaterialResponse> Materials { get; set; } = new();
    }

    public class StudentLearningMaterialResponse
    {
        public Guid LessonItemId { get; set; }
        public int Type { get; set; }
        public int OrderIndex { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? Content { get; set; }
        public StudentLearningQuizResponse? Quiz { get; set; }
        public StudentLearningPracticeResponse? Practice { get; set; }
    }

    public class StudentLearningPracticeResponse
    {
        public Guid GradedItemId { get; set; }
        public string? SubmissionGuidelines { get; set; }
        public int MaxScore { get; set; }
        public List<StudentLearningPracticeAttemptResponse> Attempts { get; set; } = new();
    }

    public class StudentLearningPracticeAttemptResponse
    {
        public Guid GradedAttemptId { get; set; }
        public int AttemptNumber { get; set; }
        public int Status { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public bool IsPassed { get; set; }
        public string? SubmittedText { get; set; }
        public string? Feedback { get; set; }
    }

    public class StudentLearningQuizResponse
    {
        public Guid GradedItemId { get; set; }
        public string Title { get; set; } = "Quiz";
        public List<StudentLearningQuestionResponse> Questions { get; set; } = new();
    }

    public class StudentLearningQuestionResponse
    {
        public Guid QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public List<StudentLearningAnswerOptionResponse> Options { get; set; } = new();
    }

    public class StudentLearningAnswerOptionResponse
    {
        public Guid AnswerOptionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }
}
