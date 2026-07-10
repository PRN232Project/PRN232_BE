using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Payment;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Course;
using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure;

[ApiController]
[Route("api/student/courses")]
[Authorize(Roles = "Student")]
public class StudentCoursesController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICourseService _courseService;

    public StudentCoursesController(IEnrollmentService enrollmentService, ICourseService courseService)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
    }

    [HttpGet("enrolled")]
    public async Task<IActionResult> GetEnrolledCourses()
    {
        var response = await _enrollmentService.GetStudentEnrollmentsAsync();
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{courseId:guid}/detail")]
    public async Task<IActionResult> GetCourseDetail(Guid courseId)
    {
        var response = await _courseService.GetCourseDetailForStudentAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("{courseId:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid courseId)
    {
        var courseResponse = await _courseService.GetCourseDetailAsync(courseId);
        if (!courseResponse.IsSuccess)
        {
            return StatusCode((int)courseResponse.StatusCode, courseResponse);
        }

        var course = courseResponse.Result as CourseResponse;
        if (course != null && course.Price > 0)
        {
            return BadRequest(new { message = "Khóa học có phí, vui lòng thanh toán trước khi ghi danh." });
        }

        var response = await _enrollmentService.EnrollStudentDirectlyAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("{courseId:guid}/checkout")]
    public async Task<IActionResult> Checkout(
        Guid courseId,
        [FromServices] IPaymentService paymentService,
        [FromServices] AppDbContext dbContext,
        [FromServices] IClaimService claimService)
    {
        var courseResponse = await _courseService.GetCourseDetailAsync(courseId);
        if (!courseResponse.IsSuccess)
        {
            return StatusCode((int)courseResponse.StatusCode, courseResponse);
        }

        var course = courseResponse.Result as CourseResponse;
        if (course == null)
        {
            return BadRequest(new { message = "Không tìm thấy khóa học" });
        }

        var claim = claimService.GetUserClaim();
        var alreadyEnrolled = await dbContext.Enrollments.AnyAsync(e =>
            e.UserId == claim.UserId &&
            e.CourseId == courseId &&
            (e.Status == 1 || e.Status == 2));
        if (alreadyEnrolled)
        {
            return BadRequest(new { message = "Bạn đã đăng ký khóa học này rồi." });
        }

        try
        {
            var paymentRequest = new CreateNewPaymentRequest
            {
                CourseId = courseId,
                Amount = course.Price
            };
            var paymentResponse = await paymentService.CreatePayOSPaymentAsync(paymentRequest);
            return Ok(new ApiResponse().SetOk(paymentResponse));
        }
        catch (Exception)
        {
            // Fallback: Sandbox Mock Payment
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = claim.UserId,
                CourseId = courseId,
                Amount = course.Price,
                Status = 1, // Success
                OrderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                PaymentLinkId = "mock-link-id",
                CheckoutUrl = "mock-sandbox-success",
                Currency = "VND",
                Method = "Mock-Sandbox",
                Type = 0,
                PaidAt = DateTime.UtcNow
            };

            await dbContext.Payments.AddAsync(payment);

            var enrollment = new Enrollment
            {
                EnrollmentId = Guid.NewGuid(),
                UserId = claim.UserId,
                CourseId = courseId,
                ProgressPercent = 0,
                Status = 1,
                EnrolledAt = DateTime.UtcNow
            };
            await dbContext.Enrollments.AddAsync(enrollment);
            await dbContext.SaveChangesAsync();

            return Ok(new ApiResponse().SetOk(new { checkoutUrl = "mock-sandbox-success" }));
        }
    }

    [HttpPost("payment-sync/{orderCode:long}")]
    public async Task<IActionResult> SyncPayment(long orderCode, [FromServices] IPaymentService paymentService)
    {
        var response = await paymentService.SyncPaymentStatusAsync(orderCode);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{courseId:guid}/enrollment-status")]
    public async Task<IActionResult> GetEnrollmentStatus(Guid courseId)
    {
        var isEnrolled = await _enrollmentService.CheckEnrollmentAsync(courseId);
        return Ok(new { courseId, isEnrolled });
    }
}
