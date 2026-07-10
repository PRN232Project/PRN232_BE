using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;

[ApiController]
[Route("api/student/messages")]
[Authorize(Roles = "Student")]
public class StudentMessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly IClaimService _claimService;
    private readonly IStorageService _storageService;

    public StudentMessagesController(IMessageService messageService, IClaimService claimService, IStorageService storageService)
    {
        _messageService = messageService;
        _claimService = claimService;
        _storageService = storageService;
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        var currentUserId = _claimService.GetUserClaim().UserId;
        var response = await _messageService.GetChatContactsAsync(currentUserId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpGet("conversation/{partnerId:guid}")]
    public async Task<IActionResult> GetConversation(Guid partnerId)
    {
        var currentUserId = _claimService.GetUserClaim().UserId;
        var response = await _messageService.GetConversationAsync(currentUserId, partnerId);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendStudentMessageRequest request)
    {
        if (request == null || request.ReceiverId == Guid.Empty)
        {
            return BadRequest(new { message = "ReceiverId is required." });
        }

        var currentUserId = _claimService.GetUserClaim().UserId;
        var response = await _messageService.SendMessageAsync(currentUserId, request.ReceiverId, request.Content ?? string.Empty);
        return StatusCode((int)response.StatusCode, response);
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request)
    {
        if (request?.Image == null || request.Image.Length == 0)
        {
            return BadRequest(new { message = "Image file is required." });
        }

        var imageUrl = await _storageService.UploadUserImageAsync("student", request.Image);
        return Ok(new { imageUrl });
    }

    public class SendStudentMessageRequest
    {
        public Guid ReceiverId { get; set; }
        public string? Content { get; set; }
    }

    public class UploadImageRequest
    {
        public IFormFile Image { get; set; } = default!;
    }
}
