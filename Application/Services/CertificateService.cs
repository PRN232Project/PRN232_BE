using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Certificate;
using OnlineLearningPlatformApi.Domain.Entities;

namespace OnlineLearningPlatformApi.Application.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CertificateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse> VerifyCertificateAsync(string certificateCode)
        {
            var response = new ApiResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(certificateCode))
                    return response.SetBadRequest("Mã bằng cấp không được để trống.");

                var cert = await _unitOfWork.Certificates.GetQueryable()
                    .Include(c => c.User)
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.CertificateCode == certificateCode && !c.IsDeleted);

                if (cert == null)
                    return response.SetNotFound("Không tìm thấy bằng cấp hoặc bằng cấp đã bị thu hồi.");

                var result = new CertificateVerificationResponse
                {
                    StudentName = cert.User.FullName ?? cert.User.Email,
                    CourseName = cert.Course.Title,
                    IssueDate = cert.IssueDate,
                    CertificateCode = cert.CertificateCode,
                    CertificateUrl = cert.CertificateUrl
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest($"Lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetMyCertificatesAsync(Guid userId)
        {
            var response = new ApiResponse();
            try
            {
                // Auto-healing check: Find any completed enrollment that is missing a certificate
                var completedEnrollments = await _unitOfWork.Enrollments.GetAllAsync(
                    e => e.UserId == userId && e.ProgressPercent >= 100 && !e.IsDeleted
                );

                bool hasNewCerts = false;
                foreach (var enrollment in completedEnrollments)
                {
                    var existingCert = await _unitOfWork.Certificates.GetAsync(
                        c => c.UserId == userId && c.CourseId == enrollment.CourseId && !c.IsDeleted
                    );

                    if (existingCert == null)
                    {
                        string uniqueCode = "OLP-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                        var newCert = new Certificate
                        {
                            CertificateId = Guid.NewGuid(),
                            UserId = userId,
                            CourseId = enrollment.CourseId,
                            IssueDate = enrollment.CompletedAt ?? DateTime.UtcNow,
                            CertificateCode = uniqueCode,
                            IsDeleted = false
                        };
                        await _unitOfWork.Certificates.AddAsync(newCert);
                        hasNewCerts = true;
                    }
                }

                if (hasNewCerts)
                {
                    await _unitOfWork.SaveChangeAsync();
                }

                var certificates = await _unitOfWork.Certificates.GetAllAsync(
                    c => c.UserId == userId && !c.IsDeleted,
                    include: c => c.Include(x => x.Course).Include(x => x.User)
                );

                var result = new List<CertificateResponse>();

                foreach (var cert in certificates.OrderByDescending(c => c.IssueDate))
                {
                    var instructor = await _unitOfWork.Users.GetAsync(u => u.UserId == cert.Course.CreatedBy);

                    result.Add(new CertificateResponse
                    {
                        CertificateId = cert.CertificateId,
                        CourseTitle = cert.Course.Title,
                        CourseImage = cert.Course.Image ?? "https://placehold.co/600x400/e2e8f0/475569?text=Course+Image",
                        InstructorName = instructor?.FullName ?? "CourseSphere Instructor",
                        StudentName = cert.User.FullName,
                        IssueDate = cert.IssueDate,
                        CertificateCode = cert.CertificateCode
                    });
                }

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }
    }
}