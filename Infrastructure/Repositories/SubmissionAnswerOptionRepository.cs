using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class SubmissionAnswerOptionRepository : GenericRepository<SubmissionAnswerOption>, ISubmissionAnswerOptionRepository
    {
        public SubmissionAnswerOptionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
