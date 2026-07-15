using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Requests.Admin;
using OnlineLearningPlatformApi.Application.Responses.Admin;
using OnlineLearningPlatformApi.Application.Responses.Payment;


namespace OnlineLearningPlatformApi.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        private readonly IPaymentService _paymentService;

        public AdminService(IUnitOfWork uow, IPaymentService paymentService)
        {
            _uow = uow;
            _paymentService = paymentService;
        }

        public async Task<AdminOverviewResponse> GetOverviewAsync(int recentPayments = 10, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var response = new AdminOverviewResponse();

            DateTime? start = fromDate?.Date;
            DateTime? end = toDate?.Date.AddDays(1).AddTicks(-1);

            if (start.HasValue)
            {
                start = DateTime.SpecifyKind(start.Value, DateTimeKind.Utc);
            }
            if (end.HasValue)
            {
                end = DateTime.SpecifyKind(end.Value, DateTimeKind.Utc);
            }

            var users = await _uow.Users.GetAllAsync(u => true);
            var courses = await _uow.Courses.GetAllAsync(c => !c.IsDeleted);
            
            var enrollments = await _uow.Enrollments.GetAllAsync(e => 
                !e.IsDeleted &&
                (start == null || (e.EnrolledAt.HasValue && e.EnrolledAt.Value >= start)) &&
                (end == null || (e.EnrolledAt.HasValue && e.EnrolledAt.Value <= end))
            );

            var query = _uow.Payments.GetQueryable();
            if (start.HasValue)
            {
                query = query.Where(p => (p.Status == 1 && p.PaidAt != null && p.PaidAt.Value >= start.Value) || (p.Status != 1 && p.CreatedAt >= start.Value));
            }
            if (end.HasValue)
            {
                query = query.Where(p => (p.Status == 1 && p.PaidAt != null && p.PaidAt.Value <= end.Value) || (p.Status != 1 && p.CreatedAt <= end.Value));
            }

            var allPayments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.User)
                .Include(p => p.Course)
                .ToListAsync();

            // TotalRevenue chỉ tính Paid (Status == 1)
            var paidPayments = allPayments.Where(p => p.Status == 1).ToList();

            response.TotalUsers = users.Count(u => 
                (start == null || u.CreatedAt >= start) &&
                (end == null || u.CreatedAt <= end)
            );
            response.TotalCourses = courses.Count(c => 
                (start == null || c.CreatedAt >= start) &&
                (end == null || c.CreatedAt <= end)
            );
            response.TotalEnrollments = enrollments.Count;
            response.TotalRevenue = paidPayments.Sum(p => p.Amount);

            var periodTopCourse = enrollments
                .GroupBy(e => e.CourseId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            if (periodTopCourse != null)
            {
                var course = courses.FirstOrDefault(c => c.CourseId == periodTopCourse.Key);
                response.TopCourseTitle = course?.Title ?? "Khóa học không tên";
                response.TopCourseEnrolls = periodTopCourse.Count();
            }
            else
            {
                response.TopCourseTitle = "Chưa có lượt đăng ký";
                response.TopCourseEnrolls = 0;
            }

            var periodTopInstructor = enrollments
                .Select(e => new { Enrollment = e, Course = courses.FirstOrDefault(c => c.CourseId == e.CourseId) })
                .Where(x => x.Course != null)
                .GroupBy(x => x.Course!.CreatedBy)
                .OrderByDescending(g => g.Select(x => x.Enrollment.UserId).Distinct().Count())
                .FirstOrDefault();

            if (periodTopInstructor != null)
            {
                var inst = users.FirstOrDefault(u => u.UserId == periodTopInstructor.Key);
                response.TopInstructorName = inst?.FullName ?? "Giảng viên không tên";
                response.TopInstructorStudents = periodTopInstructor.Select(x => x.Enrollment.UserId).Distinct().Count();
            }
            else
            {
                response.TopInstructorName = "Chưa có giảng viên hoạt động";
                response.TopInstructorStudents = 0;
            }

            response.RecentPayments = allPayments
                .Take(recentPayments)
                .Select(p => new PaymentRecord
                {
                    PaymentId = p.PaymentId,
                    OrderCode = p.OrderCode,
                    StudentName = p.User?.FullName,
                    UserEmail = p.User?.Email,
                    CourseTitle = p.Course?.Title,
                    Amount = p.Amount,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    ExpiredAt = p.ExpiredAt,
                    PaidAt = p.PaidAt
                }).ToList();

            // 1. Top Courses by Revenue
            response.TopCoursesByRevenue = paidPayments
                .GroupBy(p => p.CourseId)
                .Select(g => {
                    var course = courses.FirstOrDefault(c => c.CourseId == g.Key);
                    return new CourseStatRecord
                    {
                        CourseId = g.Key ?? Guid.Empty,
                        Title = course?.Title ?? "Khóa học không tên",
                        Revenue = g.Sum(p => p.Amount),
                        EnrollCount = g.Count()
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            // 2. Top Instructors by Revenue (Select from all instructors in DB to show all, even if 0 revenue)
            var activeInstructors = users.Where(u => u.Role == 1 && !u.IsDeleted).ToList();
            response.TopInstructorsByRevenue = activeInstructors
                .Select(inst => {
                    var instPayments = paidPayments.Where(p => p.Course?.CreatedBy == inst.UserId).ToList();
                    return new InstructorStatRecord
                    {
                        InstructorId = inst.UserId,
                        InstructorName = inst.FullName,
                        Email = inst.Email,
                        Revenue = instPayments.Sum(p => p.Amount),
                        StudentCount = instPayments.Select(p => p.UserId).Distinct().Count()
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            // 3. Top Courses by Enrollment (Bán chạy nhất)
            response.TopCoursesByEnrollment = paidPayments
                .GroupBy(p => p.CourseId)
                .Select(g => {
                    var course = courses.FirstOrDefault(c => c.CourseId == g.Key);
                    return new CourseStatRecord
                    {
                        CourseId = g.Key ?? Guid.Empty,
                        Title = course?.Title ?? "Khóa học không tên",
                        Revenue = g.Sum(p => p.Amount),
                        EnrollCount = g.Count()
                    };
                })
                .OrderByDescending(x => x.EnrollCount)
                .Take(5)
                .ToList();

            // 4. Top Instructors by Enrollment (Xuất sắc nhất - Select from all instructors in DB to show all, even if 0 students)
            response.TopInstructorsByEnrollment = activeInstructors
                .Select(inst => {
                    var instPayments = paidPayments.Where(p => p.Course?.CreatedBy == inst.UserId).ToList();
                    return new InstructorStatRecord
                    {
                        InstructorId = inst.UserId,
                        InstructorName = inst.FullName,
                        Email = inst.Email,
                        Revenue = instPayments.Sum(p => p.Amount),
                        StudentCount = instPayments.Select(p => p.UserId).Distinct().Count()
                    };
                })
                .OrderByDescending(x => x.StudentCount)
                .Take(5)
                .ToList();

            // 5. Top Students by Spending (Mua khóa học nhiều nhất)
            response.TopStudentsBySpending = paidPayments
                .GroupBy(p => p.UserId)
                .Select(g => {
                    var student = users.FirstOrDefault(u => u.UserId == g.Key);
                    return new StudentStatRecord
                    {
                        UserId = g.Key,
                        FullName = student?.FullName ?? "Học viên không tên",
                        Email = student?.Email ?? "---",
                        CourseCount = g.Count(),
                        TotalSpent = g.Sum(p => p.Amount)
                    };
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .ToList();

            // 6. Top Students by Enrollment (Join khóa học nhiều nhất)
            response.TopStudentsByEnrollment = enrollments
                .Where(e => !e.IsDeleted)
                .GroupBy(e => e.UserId)
                .Select(g => {
                    var student = users.FirstOrDefault(u => u.UserId == g.Key);
                    var studentPayments = paidPayments.Where(p => p.UserId == g.Key).ToList();
                    return new StudentStatRecord
                    {
                        UserId = g.Key,
                        FullName = student?.FullName ?? "Học viên không tên",
                        Email = student?.Email ?? "---",
                        CourseCount = g.Count(),
                        TotalSpent = studentPayments.Sum(p => p.Amount)
                    };
                })
                .OrderByDescending(x => x.CourseCount)
                .Take(5)
                .ToList();

            return response;
        }


        public async Task<AdminDashboardResponse> GetDashboardAsync(int year, int? month = null, int? day = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var response = new AdminDashboardResponse();

            // Xác định khoảng thời gian
            DateTime start, end;
            if (fromDate.HasValue && toDate.HasValue)
            {
                start = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                end = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            }
            else
            {
                start = DateTime.SpecifyKind(new DateTime(year, 1, 1), DateTimeKind.Utc);
                end = DateTime.SpecifyKind(new DateTime(year, 12, 31, 23, 59, 59), DateTimeKind.Utc);
            }

            var allPayments = await _uow.Payments.GetAllAsync(p =>
                p.Status == 1 && p.PaidAt != null &&
                p.PaidAt.Value >= start && p.PaidAt.Value <= end);

            var allEnrollments = await _uow.Enrollments.GetAllAsync(e =>
                !e.IsDeleted && e.EnrolledAt != null &&
                e.EnrolledAt.Value >= start && e.EnrolledAt.Value <= end);

            var totalDays = (end - start).TotalDays;

            DateTime prevStart, prevEnd;
            if (fromDate.HasValue && toDate.HasValue)
            {
                var duration = end - start;
                prevStart = DateTime.SpecifyKind(start - duration, DateTimeKind.Utc);
                prevEnd = DateTime.SpecifyKind(start.AddTicks(-1), DateTimeKind.Utc);
            }
            else
            {
                prevStart = DateTime.SpecifyKind(new DateTime(year - 1, 1, 1), DateTimeKind.Utc);
                prevEnd = DateTime.SpecifyKind(new DateTime(year - 1, 12, 31, 23, 59, 59), DateTimeKind.Utc);
            }

            var prevPayments = await _uow.Payments.GetAllAsync(p =>
                p.Status == 1 && p.PaidAt != null &&
                p.PaidAt.Value >= prevStart && p.PaidAt.Value <= prevEnd);

            var prevEnrollments = await _uow.Enrollments.GetAllAsync(e =>
                !e.IsDeleted && e.EnrolledAt != null &&
                e.EnrolledAt.Value >= prevStart && e.EnrolledAt.Value <= prevEnd);

            var currentRevenue = allPayments.Sum(p => p.Amount);
            var prevRevenue = prevPayments.Sum(p => p.Amount);
            var currentEnrolls = allEnrollments.Count();
            var prevEnrolls = prevEnrollments.Count();

            response.RevenueGrowth = prevRevenue == 0 ? null : Math.Round((currentRevenue - prevRevenue) / prevRevenue * 100, 1);
            response.EnrollmentGrowth = prevEnrolls == 0 ? null : Math.Round((decimal)(currentEnrolls - prevEnrolls) / prevEnrolls * 100, 1);

            var daysList = new List<DateTime>();
            var loopStart = start.Date;
            var today = DateTime.UtcNow.Date;
            var loopEnd = end.Date > today ? today : end.Date;

            if (loopStart <= loopEnd)
            {
                var loopDays = (loopEnd - loopStart).Days;
                if (loopDays > 366)
                {
                    var step = (int)Math.Ceiling(loopDays / 300.0);
                    for (var d = loopStart; d <= loopEnd; d = d.AddDays(step))
                    {
                        daysList.Add(d);
                    }
                }
                else
                {
                    for (var d = loopStart; d <= loopEnd; d = d.AddDays(1))
                    {
                        daysList.Add(d);
                    }
                }
            }

            response.RevenueMonths = daysList.Select(d => d.ToString("dd/MM/yyyy")).ToList();
            response.RevenueData = daysList
                .Select(d => allPayments.Where(p => p.PaidAt!.Value.Date == d).Sum(p => p.Amount))
                .ToList();

            response.EnrollmentMonths = response.RevenueMonths;
            response.EnrollmentData = daysList
                .Select(d => allEnrollments.Count(e => e.EnrolledAt!.Value.Date == d))
                .ToList();

            var users = await _uow.Users.GetAllAsync(u => 
                !u.IsDeleted &&
                u.CreatedAt >= start &&
                u.CreatedAt <= end
            );
            response.AdminCount = users.Count(u => u.Role == 0);
            response.InstructorCount = users.Count(u => u.Role == 1);
            response.StudentCount = users.Count(u => u.Role == 2);

            var allEnrollmentsTotal = await _uow.Enrollments.GetAllAsync(e => !e.IsDeleted);
            var allCourses = await _uow.Courses.GetAllAsync(c => !c.IsDeleted);
            var top5 = allEnrollmentsTotal
                .GroupBy(e => e.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).Take(5).ToList();

            foreach (var item in top5)
            {
                var course = allCourses.FirstOrDefault(c => c.CourseId == item.CourseId);
                if (course == null) continue;
                var title = course.Title.Length > 30 ? course.Title[..30] + "..." : course.Title;
                response.TopCourseTitles.Add(title);
                response.TopCourseEnrolls.Add(item.Count);
            }
            return response;
        }




        public async Task<AdminUsersResponse> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, string? role = null)
        {
            var all = await _uow.Users.GetAllAsync(u => !u.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                all = all.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "all")
            {
                var roleMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    { { "Admin", 0 }, { "Instructor", 1 }, { "Student", 2 } };
                if (roleMap.TryGetValue(role, out var rv))
                    all = all.Where(u => u.Role == rv).ToList();
            }

            var total = all.Count;
            var paged = all
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToItem)
                .ToList();

            return new AdminUsersResponse { Users = paged, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<AdminUserItem?> GetUserByIdAsync(Guid userId)
        {
            var user = await _uow.Users.GetAsync(u => u.UserId == userId && !u.IsDeleted);
            return user == null ? null : MapToItem(user);
        }

        public async Task<bool> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request)
        {
            var user = await _uow.Users.GetAsync(u => u.UserId == userId && !u.IsDeleted);
            if (user == null) return false;
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.Bio = request.Bio;
            user.Title = request.Title;
            user.Role = request.Role;
            user.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(user);
            await _uow.SaveChangeAsync();
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(Guid userId)
        {
            var user = await _uow.Users.GetAsync(u => u.UserId == userId && !u.IsDeleted);
            if (user == null) return false;
            user.IsDeleted = true;
            user.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(user);
            await _uow.SaveChangeAsync();
            return true;
        }

        public async Task<bool> ToggleBanUserAsync(Guid userId)
        {
            var user = await _uow.Users.GetAsync(u => u.UserId == userId && !u.IsDeleted);
            if (user == null) return false;
            user.IsVerfied = !user.IsVerfied;
            user.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(user);
            await _uow.SaveChangeAsync();
            return true;
        }

        private static AdminUserItem MapToItem(OnlineLearningPlatformApi.Domain.Entities.User user) => new()
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Image = user.Image,
            PhoneNumber = user.PhoneNumber,
            Bio = user.Bio,
            Title = user.Title,
            Role = user.Role,
            RoleName = user.Role switch { 0 => "Admin", 1 => "Instructor", 2 => "Student", _ => "User" },
            IsVerified = user.IsVerfied,
            IsDeleted = user.IsDeleted,
            CreatedAt = user.CreatedAt.ToLocalTime()
        };
    }
}