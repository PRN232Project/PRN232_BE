using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Responses.Course;

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

    [HttpGet("{courseId:guid}/enrollment-status")]
    public async Task<IActionResult> GetEnrollmentStatus(Guid courseId)
    {
        var isEnrolled = await _enrollmentService.CheckEnrollmentAsync(courseId);
        return Ok(new { courseId, isEnrolled });
    }
}
