using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;
using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class LessonItemRepository : GenericRepository<LessonItem>, ILessonItemRepository
    {
        public LessonItemRepository(AppDbContext context) : base(context)
        {
        }
    }
}
