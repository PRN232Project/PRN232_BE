using OnlineLearningPlatformApi.Domain;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Infrastructure.IRepositories;

namespace OnlineLearningPlatformApi.Infrastructure.Repositories
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(AppDbContext context) : base(context)
        {
        }
    }
}