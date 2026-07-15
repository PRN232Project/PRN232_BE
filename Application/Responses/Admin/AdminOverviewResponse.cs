using OnlineLearningPlatformApi.Application.Responses.Payment;
using System.Collections.Generic;

namespace OnlineLearningPlatformApi.Application.Responses.Admin
{
    public class CourseStatRecord
    {
        public Guid CourseId { get; set; }
        public string? Title { get; set; }
        public int EnrollCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class InstructorStatRecord
    {
        public Guid InstructorId { get; set; }
        public string? InstructorName { get; set; }
        public string? Email { get; set; }
        public int StudentCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class StudentStatRecord
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int CourseCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class AdminOverviewResponse
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }

        public string? TopCourseTitle { get; set; }
        public int TopCourseEnrolls { get; set; }
        public string? TopInstructorName { get; set; }
        public int TopInstructorStudents { get; set; }

        public List<PaymentRecord>? RecentPayments { get; set; }

        public List<CourseStatRecord> TopCoursesByRevenue { get; set; } = new();
        public List<InstructorStatRecord> TopInstructorsByRevenue { get; set; } = new();
        public List<CourseStatRecord> TopCoursesByEnrollment { get; set; } = new();
        public List<InstructorStatRecord> TopInstructorsByEnrollment { get; set; } = new();
        public List<StudentStatRecord> TopStudentsBySpending { get; set; } = new();
        public List<StudentStatRecord> TopStudentsByEnrollment { get; set; } = new();
    }
}