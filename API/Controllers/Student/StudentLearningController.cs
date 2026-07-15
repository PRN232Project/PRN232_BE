using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;

[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentLearningController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IUserLessonProgressService _progressService;
    private readonly IGradedAttemptService _gradedAttemptService;

    public StudentLearningController(ICourseService courseService, IUserLessonProgressService progressService, IGradedAttemptService gradedAttemptService)
    {
        _courseService = courseService;
        _progressService = progressService;
        _gradedAttemptService = gradedAttemptService;
    }

    [HttpGet("courses/{courseId:guid}/learning")]
    public async Task<IActionResult> GetLearning(Guid courseId)
    {
        var response = await _courseService.GetCourseDetailForStudentAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("lessons/{lessonId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid lessonId)
    {
        var response = await _progressService.MarkLessonCompletedAsync(lessonId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("practice/submit")]
    public async Task<IActionResult> SubmitPractice([FromBody] OnlineLearningPlatformApi.Application.Requests.Practice.SubmitPracticeAttemptRequest request)
    {
        var response = await _gradedAttemptService.SubmitPracticeAttemptAsync(request);
        return StatusCode((int)response.StatusCode, response);
    }
}
