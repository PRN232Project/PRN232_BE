using Microsoft.AspNetCore.Mvc;
using OnlineLearningPlatformApi.Application.IServices;

[ApiController]
[Route("Payment")]
public class PaymentRedirectController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentRedirectController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// PayOS redirects here after successful payment.
    /// Syncs payment status then redirects to React Frontend.
    /// </summary>
    [HttpGet("Success")]
    public async Task<IActionResult> Success(
        [FromQuery] string? code,
        [FromQuery] string? id,
        [FromQuery] bool? cancel,
        [FromQuery] string? status,
        [FromQuery] long? orderCode)
    {
        if (code == "00" && orderCode.HasValue)
        {
            try
            {
                await _paymentService.SyncPaymentStatusAsync(orderCode.Value);
            }
            catch
            {
                // Webhook may have already processed it
            }
        }

        var frontendUrl = $"http://localhost:3000/payment/success?code={code}&id={id}&cancel={cancel}&status={status}&orderCode={orderCode}";
        return Redirect(frontendUrl);
    }

    /// <summary>
    /// PayOS redirects here when payment is cancelled or fails.
    /// </summary>
    [HttpGet("Fail")]
    public IActionResult Fail(
        [FromQuery] string? code,
        [FromQuery] string? id,
        [FromQuery] bool? cancel,
        [FromQuery] string? status,
        [FromQuery] long? orderCode)
    {
        var frontendUrl = $"http://localhost:3000/payment/fail?code={code}&id={id}&cancel={cancel}&status={status}&orderCode={orderCode}";
        return Redirect(frontendUrl);
    }
}
