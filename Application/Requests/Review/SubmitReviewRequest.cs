namespace OnlineLearningPlatformApi.Application.Requests.Review;

public class SubmitReviewRequest
{
    public Guid CourseId { get; set; }
    public int Rating { get; set; } // 1–5
    public string? Comment { get; set; }
}
