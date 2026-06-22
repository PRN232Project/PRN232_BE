using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class LessonResourceRepository : GenericRepository<LessonResource>, ILessonResourceRepository
    {
        public LessonResourceRepository(AppDbContext context) : base(context)
        {
        }
    }
}
