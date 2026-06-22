using OnlineLearningPlatformApi.Infrastructure.IRepositories;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Domain;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class GradedAttemptRepository : GenericRepository<GradedAttempt>, IGradedAttemptRepository
    {
        public GradedAttemptRepository(AppDbContext context) : base(context)
        {
        }
    }
}
