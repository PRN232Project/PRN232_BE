namespace OnlineLearningPlatformApi.Domain.Entities;

public class CourseReview
{
    public Guid CourseReviewId { get; set; }
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public virtual Course Course { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
