using OnlineLearningPlatformApi.Application.Requests.Review;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices;

public interface ICourseReviewService
{
    Task<ApiResponse> GetReviewsByCourseAsync(Guid courseId);
    Task<ApiResponse> SubmitReviewAsync(SubmitReviewRequest request);
    Task<ApiResponse> DeleteReviewAsync(Guid reviewId);
}
