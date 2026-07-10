using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.UserLessonProgress;

[ApiController]
[Route("api/student/progress")]
[Authorize(Roles = "Student")]
public class StudentProgressController : ControllerBase
{
    private readonly IUserLessonProgressService _progressService;
    private readonly IClaimService _claimService;

    public StudentProgressController(IUserLessonProgressService progressService, IClaimService claimService)
    {
        _progressService = progressService;
        _claimService = claimService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProgress()
    {
        var currentUserId = _claimService.GetUserClaim().UserId;
        var response = await _progressService.GetLessonProgressByUserAsync(currentUserId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{lessonId:guid}")]
    public async Task<IActionResult> GetProgressByLesson(Guid lessonId)
    {
        var response = await _progressService.GetLessonProgressAsync(lessonId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateUserLessonProgressRequest request)
    {
        var response = await _progressService.StartOrUpdateProgressAsync(request);
        return StatusCode((int)response.StatusCode, response);
    }
}
