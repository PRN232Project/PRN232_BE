using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Infrastructure.IRepositories
{
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        IQueryable<Enrollment> GetQueryable();
    }
}
