using OnlineLearningPlatformApi.Application.Responses;
using Microsoft.AspNetCore.Http;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IGradedAttemptService
    {
        Task<ApiResponse> StartAttemptAsync(Guid gradedItemId);
        Task<ApiResponse> SubmitShortAnswerAsync(Guid attemptId, Guid questionId, string answer, IFormFile? file);
        Task<ApiResponse> SubmitAttemptAsync(Guid attemptId);
        Task<ApiResponse> GradeAssignmentAsync(Guid attemptId, decimal score);
    }
}
