using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Admin;
using OnlineLearningPlatformApi.Application.Requests.Course;
using OnlineLearningPlatformApi.Application.Responses;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ICourseService _courseService;
    private readonly ILogger<AdminController> _logger;
    private readonly IWalletService _walletService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public AdminController(
        IAdminService adminService,
        ICourseService courseService,
        ILogger<AdminController> logger,
        IWalletService walletService,
        IHubContext<NotificationHub> hubContext)
    {
        _adminService = adminService;
        _courseService = courseService;
        _logger = logger;
        _walletService = walletService;
        _hubContext = hubContext;
    }

    // Dashboard and reports
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] int recentPayments = 10)
    {
        var overview = await _adminService.GetOverviewAsync(recentPayments);
        return Ok(overview);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? day,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var dashboard = await _adminService.GetDashboardAsync(
            year ?? DateTime.UtcNow.Year,
            month,
            day,
            fromDate,
            toDate);

        return Ok(dashboard);
    }

    [HttpGet("revenue/platform")]
    public async Task<IActionResult> GetPlatformRevenue()
    {
        var response = await _walletService.GetPlatformRevenueAsync();
        return FromApiResponse(response);
    }

    [HttpGet("cashflow")]
    public async Task<IActionResult> GetCashflowReport()
    {
        var response = await _walletService.GetCashflowReportAsync();
        return FromApiResponse(response);
    }

    // User management
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        var users = await _adminService.GetUsersAsync(page, pageSize, search, role);
        return Ok(users);
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var user = await _adminService.GetUserByIdAsync(userId);
        return OkOrNotFound(user, userId);
    }

    [HttpPut("users/{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserRequest request)
    {
        var updated = await _adminService.UpdateUserAsync(userId, request);
        return NoContentOrNotFound(updated, userId);
    }

    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        var deleted = await _adminService.SoftDeleteUserAsync(userId);
        return NoContentOrNotFound(deleted, userId);
    }

    [HttpPatch("users/{userId:guid}/toggle-ban")]
    public async Task<IActionResult> ToggleBanUser(Guid userId)
    {
        var toggled = await _adminService.ToggleBanUserAsync(userId);
        return NoContentOrNotFound(toggled, userId);
    }

    // Course review
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses([FromQuery] int status = -1)
    {
        var response = await _courseService.GetAllCourseForAdminAsync(status);
        return FromApiResponse(response);
    }

    [HttpGet("courses/pending")]
    public async Task<IActionResult> GetPendingCourses()
    {
        var response = await _courseService.GetPendingCoursesForAdminAsync();
        return FromApiResponse(response);
    }

    [HttpGet("courses/{courseId:guid}/review")]
    public async Task<IActionResult> GetCourseForReview(Guid courseId)
    {
        var response = await _courseService.GetCourseForEditAsync(courseId);
        return FromApiResponse(response);
    }

    [HttpPost("courses/review")]
    public async Task<IActionResult> ReviewCourse([FromBody] ApproveCourseRequest request)
    {
        var response = await _courseService.ApproveCourseAsync(request);
        if (response.IsSuccess && response.Result != null)
        {
            await _hubContext.Clients.All.SendAsync("CoursePendingUpdate");

            try
            {
                dynamic data = response.Result;
                string instructorId = data.InstructorId;
                string courseTitle = data.CourseTitle;
                string status = data.Status;

                string title = status == "Approved" ? "Khóa học đã được phê duyệt" : "Khóa học bị từ chối";
                string message = status == "Approved"
                    ? $"Khóa học '{courseTitle}' của bạn đã được phê duyệt và xuất bản thành công."
                    : $"Khóa học '{courseTitle}' của bạn bị từ chối. Lý do: {data.Reason}";

                await NotificationHub.SendNotificationToUser(_hubContext, instructorId, "course_review", title, message, new { courseId = request.CourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi thông báo SignalR cho giảng viên");
            }
        }
        return FromApiResponse(response);
    }

    [HttpGet("courses/{courseId:guid}/detail")]
    public async Task<IActionResult> GetCourseDetailForAdmin(Guid courseId)
    {
        var response = await _courseService.GetCourseDetailForAdminAsync(courseId);
        return FromApiResponse(response);
    }

    // Payout review
    [HttpGet("payouts/pending")]
    public async Task<IActionResult> GetPendingPayouts()
    {
        var response = await _walletService.GetPendingPayoutsAsync();
        return FromApiResponse(response);
    }

    [HttpPost("payouts/{transactionId:guid}/approve")]
    public async Task<IActionResult> ApprovePayout(Guid transactionId)
    {
        var response = await _walletService.ApprovePayoutAsync(transactionId);
        if (response.IsSuccess)
        {
            await _hubContext.Clients.All.SendAsync("WalletBalanceUpdate");
        }
        return FromApiResponse(response);
    }

    [HttpPost("payouts/{transactionId:guid}/reject")]
    public async Task<IActionResult> RejectPayout(Guid transactionId)
    {
        var response = await _walletService.RejectPayoutAsync(transactionId);
        if (response.IsSuccess)
        {
            await _hubContext.Clients.All.SendAsync("WalletBalanceUpdate");
        }
        return FromApiResponse(response);
    }

    private ObjectResult FromApiResponse(ApiResponse response, [CallerMemberName] string actionName = "")
    {
        if (!response.IsSuccess)
        {
            _logger.LogWarning(
                "Admin action {ActionName} returned {StatusCode}: {ErrorMessage}",
                actionName,
                response.StatusCode,
                response.ErrorMessage ?? response.Result);
        }

        return StatusCode((int)response.StatusCode, response);
    }

    private IActionResult NoContentOrNotFound(
        bool succeeded,
        Guid resourceId,
        [CallerMemberName] string actionName = "")
    {
        if (succeeded)
        {
            return NoContent();
        }

        return LogAndReturnNotFound(resourceId, actionName);
    }

    private IActionResult OkOrNotFound<T>(
        T? value,
        Guid resourceId,
        [CallerMemberName] string actionName = "")
    {
        if (value is not null)
        {
            return Ok(value);
        }

        return LogAndReturnNotFound(resourceId, actionName);
    }

    private NotFoundObjectResult LogAndReturnNotFound(Guid resourceId, string actionName)
    {
        _logger.LogWarning(
            "Admin action {ActionName} could not find resource {ResourceId}",
            actionName,
            resourceId);

        return NotFound(new
        {
            message = "Resource not found.",
            resourceId
        });
    }
}
