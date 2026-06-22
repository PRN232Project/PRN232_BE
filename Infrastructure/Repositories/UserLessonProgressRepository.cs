using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class UserLessonProgressRepository : GenericRepository<UserLessonProgress>, IUserLessonProgressRepository
    {
        public UserLessonProgressRepository(AppDbContext context) : base(context)
        {
        }
    }
}
