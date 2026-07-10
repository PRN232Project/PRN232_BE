using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;

[ApiController]
[Route("api/student/certificates")]
[Authorize(Roles = "Student")]
public class StudentCertificatesController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly IClaimService _claimService;

    public StudentCertificatesController(ICertificateService certificateService, IClaimService claimService)
    {
        _certificateService = certificateService;
        _claimService = claimService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCertificates()
    {
        var currentUserId = _claimService.GetUserClaim().UserId;
        var response = await _certificateService.GetMyCertificatesAsync(currentUserId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("verify/{certificateCode}")]
    public async Task<IActionResult> VerifyCertificate(string certificateCode)
    {
        var response = await _certificateService.VerifyCertificateAsync(certificateCode);
        return StatusCode((int)response.StatusCode, response);
    }
}
