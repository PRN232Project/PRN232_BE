namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface INotificationService
    {
        Task NotifyAdminNewCourseSubmittedAsync(string courseTitle);
    }
}