using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Infrastructure.IRepositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<List<Payment>> GetExpired();
        Task<List<Payment>> GetRecentForAdminAsync(int take);
    }
}
