using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Review;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Review;
using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Application.Services;

public class CourseReviewService : ICourseReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimService _claimService;

    public CourseReviewService(IUnitOfWork unitOfWork, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _claimService = claimService;
    }

    public async Task<ApiResponse> GetReviewsByCourseAsync(Guid courseId)
    {
        var response = new ApiResponse();
        try
        {
            var reviews = (await _unitOfWork.CourseReviews.GetAllAsync(r => r.CourseId == courseId && !r.IsDeleted))
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
            var users = (await _unitOfWork.Users.GetAllAsync(u => userIds.Contains(u.UserId)))
                .ToDictionary(u => u.UserId);

            // Current user's review if logged in
            ReviewResponse? myReview = null;
            Guid? currentUserId = null;
            try
            {
                var claim = _claimService.GetUserClaim();
                currentUserId = claim.UserId;
            }
            catch { /* anonymous */ }

            var reviewList = reviews.Select(r =>
            {
                users.TryGetValue(r.UserId, out var user);
                return new ReviewResponse
                {
                    ReviewId = r.CourseReviewId,
                    CourseId = r.CourseId,
                    UserId = r.UserId,
                    StudentName = user?.FullName ?? "Học viên",
                    StudentAvatar = user?.Image,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                };
            }).ToList();

            if (currentUserId.HasValue)
                myReview = reviewList.FirstOrDefault(r => r.UserId == currentUserId.Value);

            var avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;

            var distribution = new Dictionary<int, int>
            {
                { 1, reviews.Count(r => r.Rating == 1) },
                { 2, reviews.Count(r => r.Rating == 2) },
                { 3, reviews.Count(r => r.Rating == 3) },
                { 4, reviews.Count(r => r.Rating == 4) },
                { 5, reviews.Count(r => r.Rating == 5) }
            };

            var summary = new CourseReviewSummaryResponse
            {
                AverageRating = Math.Round(avgRating, 1),
                TotalReviews = reviews.Count,
                RatingDistribution = distribution,
                Reviews = reviewList,
                MyReview = myReview
            };

            return response.SetOk(summary);
        }
        catch (Exception ex)
        {
            return response.SetBadRequest(message: ex.Message);
        }
    }

    public async Task<ApiResponse> SubmitReviewAsync(SubmitReviewRequest request)
    {
        var response = new ApiResponse();
        try
        {
            var claim = _claimService.GetUserClaim();

            if (request.Rating < 1 || request.Rating > 5)
                return response.SetBadRequest(message: "Đánh giá phải từ 1 đến 5 sao.");

            // Check enrollment
            var enrollment = await _unitOfWork.Enrollments.GetAsync(e =>
                e.UserId == claim.UserId && e.CourseId == request.CourseId && !e.IsDeleted);
            if (enrollment == null)
                return response.SetBadRequest(message: "Bạn chưa đăng ký khóa học này.");

            // Check existing review
            var existing = await _unitOfWork.CourseReviews.GetAsync(r =>
                r.UserId == claim.UserId && r.CourseId == request.CourseId && !r.IsDeleted);

            if (existing != null)
            {
                // Update
                existing.Rating = request.Rating;
                existing.Comment = request.Comment;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.CourseReviews.Update(existing);
            }
            else
            {
                // Create
                var review = new CourseReview
                {
                    CourseReviewId = Guid.NewGuid(),
                    CourseId = request.CourseId,
                    UserId = claim.UserId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };
                await _unitOfWork.CourseReviews.AddAsync(review);
            }

            await _unitOfWork.SaveChangeAsync();
            return response.SetOk("Đánh giá của bạn đã được lưu thành công.");
        }
        catch (Exception ex)
        {
            return response.SetBadRequest(message: ex.Message);
        }
    }

    public async Task<ApiResponse> DeleteReviewAsync(Guid reviewId)
    {
        var response = new ApiResponse();
        try
        {
            var claim = _claimService.GetUserClaim();
            var review = await _unitOfWork.CourseReviews.GetAsync(r => r.CourseReviewId == reviewId && !r.IsDeleted);
            if (review == null) return response.SetNotFound("Không tìm thấy đánh giá.");

            // Only owner or admin can delete
            if (review.UserId != claim.UserId && claim.Role != 0)
                return response.SetBadRequest(message: "Bạn không có quyền xóa đánh giá này.");

            review.IsDeleted = true;
            _unitOfWork.CourseReviews.Update(review);
            await _unitOfWork.SaveChangeAsync();
            return response.SetOk("Đã xóa đánh giá.");
        }
        catch (Exception ex)
        {
            return response.SetBadRequest(message: ex.Message);
        }
    }
}
