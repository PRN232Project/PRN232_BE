namespace OnlineLearningPlatformApi.Application.Responses.Wallet
{
    public class PendingPayoutResponse
    {
        public Guid TransactionId { get; set; }
        public Guid WalletId { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public string InstructorEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankInfo { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }
}