using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Review;
using OnlineLearningPlatformApi.Application.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/reviews")]
public class CourseReviewController : ControllerBase
{
    private readonly ICourseReviewService _reviewService;

    public CourseReviewController(ICourseReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>GET reviews + rating summary for a course (public)</summary>
    [HttpGet("{courseId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(Guid courseId)
    {
        var response = await _reviewService.GetReviewsByCourseAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>Submit or update own review (Student required)</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
    {
        var response = await _reviewService.SubmitReviewAsync(request);
        return StatusCode((int)response.StatusCode, response);
    }

    /// <summary>Delete a review (owner or admin)</summary>
    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        var response = await _reviewService.DeleteReviewAsync(reviewId);
        return StatusCode((int)response.StatusCode, response);
    }
}
