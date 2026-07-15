using OnlineLearningPlatformApi.Application.Requests.Admin;
using OnlineLearningPlatformApi.Application.Responses.Admin;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IAdminService
    {
        Task<AdminOverviewResponse> GetOverviewAsync(int recentPayments = 10, DateTime? fromDate = null, DateTime? toDate = null);
        Task<AdminUsersResponse> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, string? role = null);
        Task<AdminUserItem?> GetUserByIdAsync(Guid userId);
        Task<bool> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request);
        Task<bool> SoftDeleteUserAsync(Guid userId);
        Task<bool> ToggleBanUserAsync(Guid userId);
        Task<AdminDashboardResponse> GetDashboardAsync(int year, int? month = null, int? day = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
