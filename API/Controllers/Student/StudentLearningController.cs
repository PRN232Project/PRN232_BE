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

    public StudentLearningController(ICourseService courseService, IUserLessonProgressService progressService)
    {
        _courseService = courseService;
        _progressService = progressService;
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
}
