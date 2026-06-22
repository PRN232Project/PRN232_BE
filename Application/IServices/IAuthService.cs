using OnlineLearningPlatformApi.Application.Requests.User;
using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IAuthService
    {
        Task<ApiResponse> LoginAsync(LoginRequest request);
        Task<ApiResponse> RegisterAsync(RegisterRequest request);
        Task<ApiResponse> ProfileAsync();
    }
}
