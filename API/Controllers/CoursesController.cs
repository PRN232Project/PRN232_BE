using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Course;
using OnlineLearningPlatformApi.Application.Responses;

namespace API.Controllers;

[ApiController]
[Route("api/courses")]
[AllowAnonymous]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] string? search, [FromQuery] string? languageId, [FromQuery] decimal? maxPrice)
    {
        var filter = new CourseFilterRequest
        {
            SearchTerm = search,
            PageSize = 100
        };
        var response = await _courseService.GetFilteredCoursesAsync(filter);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> GetCourseById(Guid courseId)
    {
        var response = await _courseService.GetCourseDetailAsync(courseId);
        return StatusCode((int)response.StatusCode, response);
    }
}
