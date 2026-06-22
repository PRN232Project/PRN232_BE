using OnlineLearningPlatformApi.Application.Requests.Payment;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Payment;
using PayOS.Models.Webhooks;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IPaymentService
    {
        /*        Task<ApiResponse> CreatePaymentUrlAsync(CreateNewPaymentRequest request, HttpContext context);
                Task<ApiResponse> PaymentExecuteAsync(IQueryCollection collection);*/
        Task<PaymentResponse> CreatePayOSPaymentAsync(CreateNewPaymentRequest request);
        Task HandlePayOSWebhookAsync(WebhookData data);
        Task ExpirePendingPaymentAsync();

        Task<ApiResponse> SyncPaymentStatusAsync(long orderCode);
        // Return ApiResponse where Result is List<PaymentRecord>
        Task<ApiResponse> GetSuccessfulPaymentsAsync();
        // Return simple list of PaymentRecord DTOs for presentation layer without using ApiResponse wrapper
        Task<ApiResponse> GetSuccessfulPaymentRecordsAsync();
        // Admin helpers
        Task<OnlineLearningPlatformApi.Application.Responses.Admin.TopCourseResponse> GetTopCourseByEnrollmentsAsync();
        Task<OnlineLearningPlatformApi.Application.Responses.Admin.TopInstructorResponse> GetTopInstructorByStudentsAsync();
    }
}
