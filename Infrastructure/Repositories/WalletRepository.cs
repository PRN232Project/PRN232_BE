using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
    {
        public WalletRepository(AppDbContext context) : base(context)
        {
        }
    }
}
