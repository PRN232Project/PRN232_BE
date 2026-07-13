using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Wallet;
using OnlineLearningPlatformApi.Domain.Entities;


namespace OnlineLearningPlatformApi.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _uow;
        private readonly IClaimService _claimService;

        public WalletService(IUnitOfWork uow, IClaimService claimService)
        {
            _uow = uow;
            _claimService = claimService;
        }

        public async Task<ApiResponse> GetMyWalletAsync()
        {
            var response = new ApiResponse();
            try
            {
                var userId = _claimService.GetUserClaim().UserId;

                var wallet = await _uow.Wallets.GetAsync(
                    w => w.UserId == userId,
                    include: w => w.Include(x => x.WalletTransactions.OrderByDescending(t => t.CreatedAt))
                );

                if (wallet == null)
                {
                    return response.SetOk(new WalletResponse { Balance = 0, Transactions = new List<WalletTransactionResponse>() });
                }

                var result = new WalletResponse
                {
                    WalletId = wallet.WalletId,
                    Balance = wallet.Balance,
                    PendingBalance = wallet.PendingBalance,
                    TotalEarnings = wallet.TotalEarnings,
                    TotalWithdrawn = wallet.TotalWithdrawn,
                    Status = wallet.Status,
                    Transactions = wallet.WalletTransactions.Select(t => new WalletTransactionResponse
                    {
                        TransactionId = t.WalletTransactionId,
                        Amount = t.Amount,
                        TransactionType = t.TransactionType,
                        Description = t.Description ?? "No description",
                        BalanceAfterTransaction = t.BalanceAfterTransaction,
                        CreatedAt = t.CreatedAt
                    }).ToList()
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> RequestWithdrawalAsync(decimal amount, string bankInfo)
        {
            var response = new ApiResponse();
            try
            {
                var userId = _claimService.GetUserClaim().UserId;
                await _uow.BeginTransactionAsync();

                var wallet = await _uow.Wallets.GetAsync(w => w.UserId == userId);

                if (wallet == null || wallet.Balance < amount)
                {
                    await _uow.RollbackAsync();
                    return response.SetBadRequest("Insufficient balance for this withdrawal.");
                }

                wallet.Balance -= amount;
                wallet.PendingBalance += amount;
                wallet.UpdatedAt = DateTime.UtcNow;

                var tx = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    Amount = -amount,
                    TransactionType = 1,
                    BalanceAfterTransaction = wallet.Balance,
                    Description = $"Withdrawal Request to: {bankInfo} (Pending)",
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.WalletTransactions.AddAsync(tx);
                _uow.Wallets.Update(wallet);

                await _uow.SaveChangeAsync();

                await _uow.CommitAsync();

                return response.SetOk(true);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                Console.WriteLine($"[LỖI RÚT TIỀN CỰC MẠNH]: {ex.Message}");
                Console.WriteLine($"[CHI TIẾT LỖI SÂU HƠN]: {ex.InnerException?.Message}");
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetPendingPayoutsAsync()
        {
            var response = new ApiResponse();
            try
            {
                var txs = await _uow.WalletTransactions.GetAllAsync(
                    t => t.TransactionType == 1 && t.Description != null && t.Description.Contains("(Pending)"),
                    include: q => q.Include(t => t.Wallet)
                );

                var result = new List<PendingPayoutResponse>();

                foreach (var t in txs)
                {
                    var user = await _uow.Users.GetAsync(u => u.UserId == t.Wallet.UserId);

                    var bankInfo = "";
                    if (t.Description != null)
                    {
                        var prefix = "Withdrawal Request to: ";
                        var suffix = " (Pending)";
                        var desc = t.Description;
                        if (desc.StartsWith(prefix) && desc.EndsWith(suffix))
                        {
                            bankInfo = desc.Substring(prefix.Length, desc.Length - prefix.Length - suffix.Length);
                        }
                        else
                        {
                            bankInfo = desc;
                        }
                    }

                    result.Add(new PendingPayoutResponse
                    {
                        TransactionId = t.WalletTransactionId,
                        WalletId = t.WalletId,
                        InstructorName = user?.FullName ?? "Unknown Instructor",
                        InstructorEmail = user?.Email ?? "No email",
                        Amount = Math.Abs(t.Amount),
                        BankInfo = bankInfo,
                        RequestedAt = t.CreatedAt
                    });
                }
                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCashflowReportAsync()
        {
            var response = new ApiResponse();
            try
            {
                var successfulPayments = await _uow.Payments.GetAllAsync(p => p.Status == 1);
                var totalGross = successfulPayments.Sum(p => p.Amount);
                var wallets = await _uow.Wallets.GetAllAsync();
                var totalPending = wallets.Sum(w => w.PendingBalance);
                var totalAvailable = wallets.Sum(w => w.Balance);
                var totalPaidOut = wallets.Sum(w => w.TotalWithdrawn);

                var report = new CashflowReportResponse
                {
                    TotalGrossRevenue = totalGross,
                    PlatformNetRevenue = totalGross * 0.3m,
                    InstructorTotalEarnings = totalGross * 0.7m,
                    TotalPaidOut = totalPaidOut,
                    TotalPendingPayouts = totalPending,
                    TotalAvailableInWallets = totalAvailable,
                    PlatformCashOnHand = totalGross - totalPaidOut
                };

                return response.SetOk(report);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> ApprovePayoutAsync(Guid transactionId)
        {
            var response = new ApiResponse();
            try
            {
                await _uow.BeginTransactionAsync();

                var tx = await _uow.WalletTransactions.GetAsync(
                    t => t.WalletTransactionId == transactionId,
                    include: q => q.Include(t => t.Wallet)
                );

                if (tx == null)
                {
                    await _uow.RollbackAsync();
                    return response.SetBadRequest("Transaction request not found.");
                }

                if (tx.Description == null || !tx.Description.Contains("(Pending)"))
                {
                    await _uow.RollbackAsync();
                    return response.SetBadRequest("Transaction is already processed or invalid.");
                }

                var wallet = tx.Wallet;
                var payoutAmount = Math.Abs(tx.Amount);

                if (wallet.PendingBalance < payoutAmount)
                {
                    await _uow.RollbackAsync();
                    return response.SetBadRequest("Insufficient pending balance in wallet.");
                }

                wallet.PendingBalance -= payoutAmount;
                wallet.TotalWithdrawn += payoutAmount;
                wallet.UpdatedAt = DateTime.UtcNow;

                tx.Description = tx.Description.Replace("(Pending)", "(Approved)");

                _uow.Wallets.Update(wallet);
                _uow.WalletTransactions.Update(tx);
                await _uow.SaveChangeAsync();
                await _uow.CommitAsync();

                return response.SetOk(true);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetPlatformRevenueAsync()
        {
            var response = new ApiResponse();
            try
            {
                var successfulPayments = await _uow.Payments.GetAllAsync(p => p.Status == 1);

                var totalGross = successfulPayments.Sum(p => p.Amount);
                var platformNet = totalGross * 0.3m;

                return response.SetOk(new
                {
                    TotalGrossSales = totalGross,
                    PlatformRevenue = platformNet
                });
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }
    }
}