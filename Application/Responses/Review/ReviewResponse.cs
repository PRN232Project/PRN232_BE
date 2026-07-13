namespace OnlineLearningPlatformApi.Application.Responses.Review;

public class ReviewResponse
{
    public Guid ReviewId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatar { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CourseReviewSummaryResponse
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // star → count
    public List<ReviewResponse> Reviews { get; set; } = new();
    public ReviewResponse? MyReview { get; set; } // review của current user nếu có
}
