using OnlineLearningPlatformApi.Application.Responses;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IWalletService
    {
        Task<ApiResponse> GetMyWalletAsync();

        Task<ApiResponse> RequestWithdrawalAsync(decimal amount, string bankInfo);

        Task<ApiResponse> GetPendingPayoutsAsync();

        Task<ApiResponse> ApprovePayoutAsync(Guid walletId);

        Task<ApiResponse> GetPlatformRevenueAsync();

        Task<ApiResponse> GetCashflowReportAsync();
    }
}