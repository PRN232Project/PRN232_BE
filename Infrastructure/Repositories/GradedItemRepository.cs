using OnlineLearningPlatformApi.Infrastructure.IRepositories;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Domain;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class GradedItemRepository : GenericRepository<GradedItem>, IGradedItemRepository
    {
        public GradedItemRepository(AppDbContext context) : base(context)
        {
        }
    }
}
