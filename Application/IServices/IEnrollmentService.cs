using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Requests.Enrollment;

namespace OnlineLearningPlatformApi.Application.IServices
{
    public interface IEnrollmentService
    {
        Task<ApiResponse> EnrollStudentDirectlyAsync(Guid courseId);
        Task<ApiResponse> GetStudentEnrollmentsAsync();
        Task<bool> CheckEnrollmentAsync(Guid courseId);

        Task<ApiResponse> CheckUserEnrollmentAsync(Guid userId, Guid courseId);
    }
}
