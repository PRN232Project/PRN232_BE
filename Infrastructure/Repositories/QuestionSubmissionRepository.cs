using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class QuestionSubmissionRepository : GenericRepository<QuestionSubmission>, IQuestionSubmissionRepository
    {
        public QuestionSubmissionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
