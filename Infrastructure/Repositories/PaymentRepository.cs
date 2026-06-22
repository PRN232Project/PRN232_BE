using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Payment>> GetExpired()
        {
            return await _context.Payments.Where(p => p.Status == 0 && p.ExpiredAt != null && p.ExpiredAt < DateTime.UtcNow).ToListAsync();
        }

        public async Task<List<Payment>> GetRecentForAdminAsync(int take)
        {
            return await _context.Payments
                .AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Course)
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
