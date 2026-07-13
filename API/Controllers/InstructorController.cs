using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Course;
using Microsoft.AspNetCore.SignalR;
using API.Hubs;
using OnlineLearningPlatformApi.Application.Requests.Lesson;
using OnlineLearningPlatformApi.Application.Requests.LessonItem;
using OnlineLearningPlatformApi.Application.Requests.LessonResource;
using OnlineLearningPlatformApi.Application.Requests.Module;
using OnlineLearningPlatformApi.Application.Responses;

namespace API.Controllers;

[ApiController]
[Authorize(Roles = "Instructor")]
[Route("api/[controller]")]
public class InstructorController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILessonItemService _lessonItemService;
    private readonly ILessonResourceService _lessonResourceService;
    private readonly ILessonService _lessonService;
    private readonly IModuleService _moduleService;
    private readonly IWalletService _walletService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public InstructorController(
        ICourseService courseService,
        ILessonItemService lessonItemService,
        ILessonResourceService lessonResourceService,
        ILessonService lessonService,
        IModuleService moduleService,
        IWalletService walletService,
        IHubContext<NotificationHub> hubContext)
    {
        _courseService = courseService;
        _lessonItemService = lessonItemService;
        _lessonResourceService = lessonResourceService;
        _lessonService = lessonService;
        _moduleService = moduleService;
        _walletService = walletService;
        _hubContext = hubContext;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var response = await _courseService.GetInstructorMetricsAsync();
        return FromApiResponse(response);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var response = await _courseService.GetCoursesByInstructorAsync();
        return FromApiResponse(response);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromForm] CreateNewCourseRequest request)
    {
        var response = await _courseService.CreateNewCourseAsync(request);
        return FromApiResponse(response);
    }

    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourse(Guid courseId)
    {
        var response = await _courseService.GetCourseForEditAsync(courseId);
        return FromApiResponse(response);
    }

    [HttpPut("courses/{courseId:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid courseId, [FromForm] UpdateCourseRequest request)
    {
        request.CourseId = courseId;
        var response = await _courseService.UpdateCourseAsync(request);
        return FromApiResponse(response);
    }

    [HttpDelete("courses/{courseId:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid courseId)
    {
        var response = await _courseService.DeleteCourseAsync(courseId);
        return FromApiResponse(response);
    }

    [HttpPost("courses/{courseId:guid}/submit-review")]
    public async Task<IActionResult> SubmitCourseForReview(Guid courseId)
    {
        var response = await _courseService.ValidateAndSubmitForReviewAsync(courseId);
        if (response.IsSuccess)
        {
            await _hubContext.Clients.All.SendAsync("CoursePendingUpdate");
        }
        return FromApiResponse(response);
    }

    [HttpGet("courses/{courseId:guid}/modules")]
    public async Task<IActionResult> GetModules(Guid courseId)
    {
        var response = await _moduleService.GetModulesByCourseAsync(courseId);
        return FromApiResponse(response);
    }

    [HttpPost("courses/{courseId:guid}/modules")]
    public async Task<IActionResult> CreateModule(Guid courseId, [FromBody] CreateNewModuleForCourseRequest request)
    {
        request.CourseId = courseId;
        var response = await _moduleService.CreateNewModuleForCourseAsync(request);
        return FromApiResponse(response);
    }

    [HttpGet("modules/{moduleId:guid}")]
    public async Task<IActionResult> GetModule(Guid moduleId)
    {
        var response = await _moduleService.GetModuleDetailAsync(moduleId);
        return FromApiResponse(response);
    }

    [HttpPut("modules/{moduleId:guid}")]
    public async Task<IActionResult> UpdateModule(Guid moduleId, [FromBody] UpdateModuleRequest request)
    {
        request.ModuleId = moduleId;
        var response = await _moduleService.UpdateModuleAsync(request);
        return FromApiResponse(response);
    }

    [HttpDelete("modules/{moduleId:guid}")]
    public async Task<IActionResult> DeleteModule(Guid moduleId)
    {
        var response = await _moduleService.DeleteModuleAsync(moduleId);
        return FromApiResponse(response);
    }

    [HttpGet("modules/{moduleId:guid}/lessons")]
    public async Task<IActionResult> GetLessons(Guid moduleId)
    {
        var response = await _lessonService.GetLessonsByModuleAsync(moduleId);
        return FromApiResponse(response);
    }

    [HttpPost("modules/{moduleId:guid}/lessons")]
    public async Task<IActionResult> CreateLesson(Guid moduleId, [FromBody] CreateNewLessonForModuleRequest request)
    {
        request.ModuleId = moduleId;
        var response = await _lessonService.CreateNewLessonForModuleAsync(request);
        return FromApiResponse(response);
    }

    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<IActionResult> GetLesson(Guid lessonId)
    {
        var response = await _lessonService.GetLessonDetailAsync(lessonId);
        return FromApiResponse(response);
    }

    [HttpPut("lessons/{lessonId:guid}")]
    public async Task<IActionResult> UpdateLesson(Guid lessonId, [FromBody] UpdateLessonRequest request)
    {
        var response = await _lessonService.UpdateLessonAsync(lessonId, request);
        return FromApiResponse(response);
    }

    [HttpDelete("lessons/{lessonId:guid}")]
    public async Task<IActionResult> DeleteLesson(Guid lessonId)
    {
        var response = await _lessonService.DeleteLessonAsync(lessonId);
        return FromApiResponse(response);
    }

    [HttpGet("lessons/{lessonId:guid}/items")]
    public async Task<IActionResult> GetLessonItems(Guid lessonId)
    {
        var response = await _lessonItemService.GetLessonItemsByLessonAsync(lessonId);
        return FromApiResponse(response);
    }

    [HttpPost("lessons/{lessonId:guid}/items/reading")]
    public async Task<IActionResult> CreateReadingItem(Guid lessonId, [FromBody] CreateReadingItemRequest request)
    {
        request.LessonId = lessonId;
        var response = await _lessonItemService.CreateReadingItemAsync(request);
        return FromApiResponse(response);
    }

    [HttpPost("lessons/{lessonId:guid}/items/video")]
    public async Task<IActionResult> CreateVideoItem(Guid lessonId, [FromForm] CreateVideoItemRequest request)
    {
        request.LessonId = lessonId;
        var response = await _lessonItemService.CreateVideoItemAsync(request);
        return FromApiResponse(response);
    }

    [HttpPost("lessons/{lessonId:guid}/items/quiz")]
    public async Task<IActionResult> CreateQuizItem(Guid lessonId, [FromBody] CreateQuizItemRequest request)
    {
        request.LessonId = lessonId;
        var response = await _lessonItemService.CreateQuizItemAsync(request);
        return FromApiResponse(response);
    }

    [HttpPost("lessons/{lessonId:guid}/items/writing")]
    public async Task<IActionResult> CreateWritingItem(Guid lessonId, [FromBody] CreateWritingItemRequest request)
    {
        request.LessonId = lessonId;
        var response = await _lessonItemService.CreateWritingItemAsync(request);
        return FromApiResponse(response);
    }

    [HttpPost("lessons/{lessonId:guid}/items/speaking")]
    public async Task<IActionResult> CreateSpeakingItem(Guid lessonId, [FromBody] CreateSpeakingItemRequest request)
    {
        request.LessonId = lessonId;
        var response = await _lessonItemService.CreateSpeakingItemAsync(request);
        return FromApiResponse(response);
    }

    [HttpGet("items/{lessonItemId:guid}")]
    public async Task<IActionResult> GetLessonItem(Guid lessonItemId)
    {
        var response = await _lessonItemService.GetLessonItemDetailAsync(lessonItemId);
        return FromApiResponse(response);
    }

    [HttpDelete("items/{lessonItemId:guid}")]
    public async Task<IActionResult> DeleteLessonItem(Guid lessonItemId)
    {
        var response = await _lessonItemService.DeleteLessonItemAsync(lessonItemId);
        return FromApiResponse(response);
    }

    [HttpGet("items/{lessonItemId:guid}/resources")]
    public async Task<IActionResult> GetResources(Guid lessonItemId)
    {
        var response = await _lessonResourceService.GetResourcesByLessonItemAsync(lessonItemId);
        return FromApiResponse(response);
    }

    [HttpPost("items/{lessonItemId:guid}/resources")]
    public async Task<IActionResult> CreateResource(Guid lessonItemId, [FromForm] CreateLessonResourceRequest request)
    {
        request.LessonItemId = lessonItemId;
        var response = await _lessonResourceService.CreateLessonResourceAsync(request);
        return FromApiResponse(response);
    }

    [HttpPut("resources/{resourceId:guid}")]
    public async Task<IActionResult> UpdateResource(Guid resourceId, [FromForm] UpdateLessonResourceRequest request)
    {
        var response = await _lessonResourceService.UpdateLessonResourceAsync(resourceId, request);
        return FromApiResponse(response);
    }

    [HttpDelete("resources/{resourceId:guid}")]
    public async Task<IActionResult> DeleteResource(Guid resourceId)
    {
        var response = await _lessonResourceService.DeleteLessonResourceAsync(resourceId);
        return FromApiResponse(response);
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> GetWallet()
    {
        var response = await _walletService.GetMyWalletAsync();
        return FromApiResponse(response);
    }

    [HttpPost("wallet/withdrawals")]
    public async Task<IActionResult> RequestWithdrawal([FromBody] WithdrawalRequest request)
    {
        var response = await _walletService.RequestWithdrawalAsync(request.Amount, request.BankInfo);
        if (response.IsSuccess)
        {
            await _hubContext.Clients.All.SendAsync("WalletBalanceUpdate");
        }
        return FromApiResponse(response);
    }

    private ObjectResult FromApiResponse(ApiResponse response)
    {
        return StatusCode((int)response.StatusCode, response);
    }

    public sealed class WithdrawalRequest
    {
        public decimal Amount { get; set; }
        public string BankInfo { get; set; } = string.Empty;
    }
}
