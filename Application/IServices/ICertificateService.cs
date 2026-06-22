using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface ICertificateService
    {
        Task<ApiResponse> GetMyCertificatesAsync(Guid userId);

        Task<ApiResponse> VerifyCertificateAsync(string certificateCode);
    }
}