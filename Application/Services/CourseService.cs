using OnlineLearningPlatformApi.Application.IServices;
using AutoMapper;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Application.Requests.Course;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Course;
using Microsoft.EntityFrameworkCore;


namespace OnlineLearningPlatformApi.Application.Services
{
    public class CourseService : ICourseService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IClaimService _service;
        private readonly IStorageService _storageService;
        private readonly IEmailService _emailService;
        private readonly IClaimService _claimService;

        public CourseService(IMapper mapper, IUnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService, IClaimService service, IEmailService emailService, IStorageService storageService, IClaimService claimService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
            _storageService = storageService;
            _service = service;
            _emailService = emailService;
            _claimService = claimService;
        }

        public async Task<ApiResponse> CreateNewCourseAsync(CreateNewCourseRequest request)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                var course = _mapper.Map<Course>(request);
                course.CourseId = Guid.NewGuid();
                course.CreatedBy = claim.UserId;
                course.Status = 0; // Draft
                course.CreatedAt = DateTime.UtcNow;
                course.Subtitle = request.Subtitle;
                course.Tags = request.Tags;

                if (request.ImageFile != null)
                {
                    var imageUrl = await _storageService.UploadCourseImageAsync(request.Title, request.ImageFile);
                    course.Image = imageUrl;
                }

                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.Courses.AddAsync(course);

                // Disable create default module
                //var defaultModule = new Module
                //{
                //    ModuleId = Guid.NewGuid(),
                //    CourseId = course.CourseId,
                //    Name = "Main",
                //    Description = "Default module",
                //    Index = 0,
                //    IsPublished = true,
                //    CreatedBy = claim.UserId,
                //    CreatedAt = DateTime.UtcNow
                //};
                //await _unitOfWork.Modules.AddAsync(defaultModule);
                await _unitOfWork.CommitAsync();

                return response.SetOk(course.CourseId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetActiveLanguagesAsync()
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var languages = await _unitOfWork.Languages.GetAllAsync(l => l.IsActive && !l.IsDeleted);
                var result = languages.Select(l => new
                {
                    LanguageId = l.LanguageId,
                    Name = l.Name
                }).ToList();

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetAllCourseAsync()
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var userId = _service.GetUserClaim().UserId;
                var courses = await _unitOfWork.Courses.GetAllAsync(c => c.Status == 2 && c.CreatedBy != userId && !c.Enrollments.Any(e => e.UserId == userId));
                var courseResponses = _mapper.Map<List<CourseResponse>>(courses);
                return response.SetOk(courseResponses);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseDetailAsync(Guid courseId)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null)
                {
                    return response.SetNotFound("Course not found");
                }

                // Get language name
                string languageName = "Tiếng Việt";
                if (course.LanguageId != Guid.Empty)
                {
                    var language = await _unitOfWork.Languages.GetAsync(l => l.LanguageId == course.LanguageId);
                    if (language != null) languageName = language.Name;
                }

                // Get instructor name & bio
                string instructorName = "Giảng viên";
                string instructorBio = "";
                if (course.CreatedBy != Guid.Empty)
                {
                    var instructor = await _unitOfWork.Users.GetAsync(u => u.UserId == course.CreatedBy);
                    if (instructor != null)
                    {
                        instructorName = instructor.FullName;
                        instructorBio = instructor.Bio ?? "";
                    }
                }

                // Get student count (non-deleted enrollments)
                int studentsCount = await _unitOfWork.Enrollments.CountAsync(e => 
                    e.CourseId == courseId 
                    && (e.Status == 1 || e.Status == 2) 
                    && !e.IsDeleted);

                // Get modules
                var modules = (await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted))
                    .OrderBy(m => m.Index)
                    .ToList();
                var moduleIds = modules.Select(m => m.ModuleId).ToList();

                // Get lessons
                var lessons = moduleIds.Count > 0
                    ? (await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted))
                        .OrderBy(l => l.OrderIndex).ToList()
                    : new List<Domain.Entities.Lesson>();
                var lessonIds = lessons.Select(l => l.LessonId).ToList();

                // Get lesson items
                var lessonItems = lessonIds.Count > 0
                    ? (await _unitOfWork.LessonItems.GetAllAsync(li => lessonIds.Contains(li.LessonId) && !li.IsDeleted))
                        .OrderBy(li => li.OrderIndex).ToList()
                    : new List<Domain.Entities.LessonItem>();
                var lessonItemIds = lessonItems.Select(li => li.LessonItemId).ToList();

                // Get lesson resources (to calculate video durations)
                var lessonResources = lessonItemIds.Count > 0
                    ? (await _unitOfWork.LessonResources.GetAllAsync(lr => lessonItemIds.Contains(lr.LessonItemId) && !lr.IsDeleted))
                    : new List<Domain.Entities.LessonResource>();

                // Calculate total duration (in minutes)
                int totalEstimatedMinutes = lessons.Sum(l => l.EstimatedMinutes);
                string durationStr = $"{totalEstimatedMinutes / 60} giờ {(totalEstimatedMinutes % 60)} phút";
                if (totalEstimatedMinutes < 60) durationStr = $"{totalEstimatedMinutes} phút";

                var result = new
                {
                    courseId = course.CourseId,
                    title = course.Title,
                    description = course.Description,
                    price = course.Price,
                    image = course.Image,
                    level = course.Level == 0 ? "Beginner" : course.Level == 1 ? "Intermediate" : "Advanced",
                    status = course.Status,
                    languageName,
                    instructorName,
                    instructorBio,
                    students = studentsCount,
                    duration = durationStr,
                    modules = modules.Select(m => new
                    {
                        moduleId = m.ModuleId,
                        courseId = m.CourseId,
                        title = m.Name,
                        orderIndex = m.Index,
                        lessons = lessons
                            .Where(l => l.ModuleId == m.ModuleId)
                            .Select(l => new
                            {
                                lessonId = l.LessonId,
                                moduleId = l.ModuleId,
                                title = l.Title,
                                orderIndex = l.OrderIndex,
                                lessonItems = lessonItems
                                    .Where(li => li.LessonId == l.LessonId)
                                    .Select(li => {
                                        var res = lessonResources.FirstOrDefault(lr => lr.LessonItemId == li.LessonItemId);
                                        int durationMin = 0;
                                        if (res != null) {
                                            durationMin = res.DurationInSeconds.HasValue 
                                                ? (int)Math.Ceiling(res.DurationInSeconds.Value / 60.0) 
                                                : 0;
                                        }
                                        if (durationMin == 0) durationMin = 5; // default 5 mins
                                        return new
                                        {
                                            lessonItemId = li.LessonItemId,
                                            lessonId = li.LessonId,
                                            title = res?.Title ?? (li.Type == 0 ? "Bài học Video" : li.Type == 1 ? "Bài đọc" : "Bài kiểm tra"),
                                            type = li.Type, // Video = 0, Article = 1, Quiz = 2
                                            durationMinutes = durationMin,
                                            orderIndex = li.OrderIndex
                                        };
                                    }).ToList()
                            }).ToList()
                    }).ToList()
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseDetailForAdminAsync(Guid courseId)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null)
                {
                    return response.SetNotFound("Course not found");
                }

                // Get instructor name
                string instructorName = "Unknown";
                if (course.CreatedBy != Guid.Empty)
                {
                    var instructor = await _unitOfWork.Users.GetAsync(u => u.UserId == course.CreatedBy);
                    if (instructor != null) instructorName = instructor.FullName;
                }

                var modules = (await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted))
                    .OrderBy(m => m.Index)
                    .ToList();
                var moduleIds = modules.Select(m => m.ModuleId).ToList();

                var lessons = moduleIds.Count > 0
                    ? (await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted))
                        .OrderBy(l => l.OrderIndex).ToList()
                    : new List<Domain.Entities.Lesson>();

                var result = new
                {
                    courseId = course.CourseId,
                    title = course.Title,
                    description = course.Description,
                    price = course.Price,
                    image = course.Image,
                    level = course.Level,
                    status = course.Status,
                    instructorName,
                    modules = modules.Select(m => new
                    {
                        moduleId = m.ModuleId,
                        title = m.Name,
                        index = m.Index,
                        lessons = lessons
                            .Where(l => l.ModuleId == m.ModuleId)
                            .Select(l => new
                            {
                                lessonId = l.LessonId,
                                title = l.Title,
                                description = l.Description,
                                orderIndex = l.OrderIndex,
                                estimatedMinutes = l.EstimatedMinutes
                            }).ToList()
                    }).ToList()
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> UpdateCourseAsync(UpdateCourseRequest request)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == request.CourseId && !c.IsDeleted);
                if (course == null)
                {
                    return response.SetNotFound("Course not found");
                }
                if (course.CreatedBy != claim.UserId)
                {
                    return response.SetBadRequest(message: "Bạn không có quyền cập nhật khóa học này");
                }
                if (course.Status != 0)
                {
                    return response.SetBadRequest(message: "Chỉ có thể chỉnh sửa khóa học ở trạng thái Draft");
                }

                var updatedCourse = _mapper.Map(request, course);
                if (request.ImageFile != null)
                {
                    var imageUrl = await _storageService.UploadCourseImageAsync(request.Title, request.ImageFile);
                    course.Image = imageUrl;
                }
                updatedCourse.UpdatedAt = DateTime.UtcNow;
                updatedCourse.UpdatedBy = claim.UserId;

                _unitOfWork.Courses.Update(updatedCourse);
                await _unitOfWork.SaveChangeAsync();
                return response.SetOk("Course updated successfully");
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> DeleteCourseAsync(Guid courseId)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId);
                if (course == null)
                {
                    return response.SetNotFound("Course not found");
                }
                course.IsDeleted = true;
                _unitOfWork.Courses.Update(course);
                await _unitOfWork.SaveChangeAsync();
                return response.SetOk("Course deleted successfully");
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetAllCourseForAdminAsync(int status)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                IEnumerable<Course> courses;

                if (status == -1) 
                {
                    courses = await _unitOfWork.Courses.GetAllAsync(c => !c.IsDeleted);
                }
                else if (status == 3) 
                {
                    courses = await _unitOfWork.Courses.GetAllAsync(c => !c.IsDeleted && c.Status == 0 && !string.IsNullOrEmpty(c.RejectReason));
                }
                else if (status == 0) 
                {
                    courses = await _unitOfWork.Courses.GetAllAsync(c => !c.IsDeleted && c.Status == 0 && string.IsNullOrEmpty(c.RejectReason));
                }
                else
                {
                    courses = await _unitOfWork.Courses.GetAllAsync(c => !c.IsDeleted && c.Status == status);
                }

                if (courses == null) return response.SetOk(new List<GetAllCourseForAdminResponse>());

                var result = new List<GetAllCourseForAdminResponse>();

                foreach (var course in courses)
                {
                    var modules = await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == course.CourseId);
                    var moduleIds = modules.Select(m => m.ModuleId).ToList();

                    var lessons = await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId));
                    var lessonIds = lessons.Select(l => l.LessonId).ToList();
                    var lessonItems = await _unitOfWork.LessonItems.GetAllAsync(l => lessonIds.Contains(l.LessonId));

                    var courseMapping = _mapper.Map<GetAllCourseForAdminResponse>(course);

                    
                    if (course.Status == 0 && !string.IsNullOrEmpty(course.RejectReason))
                    {
                        courseMapping.Status = 3;
                    }

                    courseMapping.ModuleCount = modules.Count();
                    courseMapping.LessonCount = lessons.Count();
                    courseMapping.VideoCount = lessonItems.Count(li => li.Type == 0);
                    courseMapping.ReadingCount = lessonItems.Count(l => l.Type == 1);

                    result.Add(courseMapping);
                }
                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetCoursesByInstructorAsync()
        {
            ApiResponse response = new ApiResponse();

            try
            {
                var claim = _service.GetUserClaim();
                var courses = await _unitOfWork.Courses
                    .GetAllAsync(c => c.CreatedBy == claim.UserId && !c.IsDeleted);

                var result = _mapper.Map<List<CourseResponse>>(courses);
                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetFilteredCoursesAsync(CourseFilterRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var query = _unitOfWork.Courses.GetQueryable()
                    .Where(c => c.Status == 2 && !c.IsDeleted);

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var searchLower = request.SearchTerm.ToLower();
                    query = query.Where(c => c.Title.ToLower().Contains(searchLower)
                                          || (c.Description != null && c.Description.ToLower().Contains(searchLower)));
                }
                
                if (!string.IsNullOrWhiteSpace(request.LanguageId))
                {
                    if (Guid.TryParse(request.LanguageId, out var parsedGuid))
                    {
                        query = query.Where(c => c.LanguageId == parsedGuid);
                    }
                    else
                    {
                        var codeToCheck = request.LanguageId.StartsWith("lang-") 
                            ? request.LanguageId.Substring(5).ToLower() 
                            : request.LanguageId.ToLower();

                        var matchedLanguages = await _unitOfWork.Languages.GetAllAsync(l => 
                            !l.IsDeleted 
                            && (l.Code.ToLower() == codeToCheck 
                                || l.Code.ToLower().Contains(codeToCheck)
                                || l.Name.ToLower().Contains(codeToCheck)));

                        var matchedIds = matchedLanguages.Select(l => l.LanguageId).ToList();
                        if (matchedIds.Any())
                        {
                            query = query.Where(c => matchedIds.Contains(c.LanguageId));
                        }
                        else
                        {
                            query = query.Where(c => c.LanguageId == Guid.Empty);
                        }
                    }
                }

                if (request.Levels != null && request.Levels.Any())
                {
                    query = query.Where(c => request.Levels.Contains(c.Level));
                }

                if (request.IsFree.HasValue)
                {
                    if (request.IsFree.Value)
                        query = query.Where(c => c.Price == 0);
                    else
                        query = query.Where(c => c.Price > 0);
                }


                query = request.SortBy.ToLower() switch
                {
                    "newest" => query.OrderByDescending(c => c.PublishedAt ?? c.CreatedAt),
                    "price_asc" => query.OrderBy(c => c.Price),
                    
                    _ => query.OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
                };

                int totalItems = await query.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                var pagedCourses = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var courseResponses = _mapper.Map<List<CourseResponse>>(pagedCourses);

                if (pagedCourses.Any())
                {
                    // Fetch real creator fullnames, enrollment counts, language names, and lesson durations
                    var instructorIds = pagedCourses.Select(c => c.CreatedBy).Distinct().ToList();
                    var instructors = await _unitOfWork.Users.GetAllAsync(u => instructorIds.Contains(u.UserId));
                    var instructorMap = instructors.ToDictionary(u => u.UserId, u => u.FullName);

                    var languageIds = pagedCourses.Select(c => c.LanguageId).Distinct().ToList();
                    var languages = await _unitOfWork.Languages.GetAllAsync(l => languageIds.Contains(l.LanguageId));
                    var languageMap = languages.ToDictionary(l => l.LanguageId, l => l.Name);

                    var courseIds = pagedCourses.Select(c => c.CourseId).ToList();

                    // Get enrollment counts
                    var enrollments = await _unitOfWork.Enrollments.GetAllAsync(e => 
                        courseIds.Contains(e.CourseId) 
                        && (e.Status == 1 || e.Status == 2) 
                        && !e.IsDeleted);
                    var enrollmentCounts = enrollments
                        .GroupBy(e => e.CourseId)
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Get lesson durations
                    var modules = await _unitOfWork.Modules.GetAllAsync(m => courseIds.Contains(m.CourseId) && !m.IsDeleted);
                    var moduleIds = modules.Select(m => m.ModuleId).ToList();

                    var lessons = moduleIds.Count > 0
                        ? await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted)
                        : new List<Domain.Entities.Lesson>();
                    
                    // Group lessons by CourseId (via ModuleId)
                    var lessonsByCourse = lessons
                        .GroupBy(l => {
                            var mod = modules.FirstOrDefault(m => m.ModuleId == l.ModuleId);
                            return mod != null ? mod.CourseId : Guid.Empty;
                        })
                        .ToDictionary(g => g.Key, g => g.Sum(l => l.EstimatedMinutes));

                    foreach (var resp in courseResponses)
                    {
                        var course = pagedCourses.First(c => c.CourseId == resp.CourseId);
                        
                        // Set level text
                        resp.Level = course.Level == 0 ? "Beginner" : course.Level == 1 ? "Intermediate" : "Advanced";
                        
                        // Set actual instructor
                        if (instructorMap.TryGetValue(course.CreatedBy, out var instName))
                        {
                            resp.InstructorName = instName;
                        }

                        // Set actual language
                        if (languageMap.TryGetValue(course.LanguageId, out var langName))
                        {
                            resp.LanguageName = langName;
                        }

                        // Set actual students
                        resp.Students = enrollmentCounts.TryGetValue(course.CourseId, out var studs) ? studs : 0;

                        // Set actual duration string
                        if (lessonsByCourse.TryGetValue(course.CourseId, out var minutes))
                        {
                            resp.Duration = $"{minutes / 60}h {minutes % 60}m";
                            if (minutes < 60) resp.Duration = $"{minutes}m";
                        }
                        else
                        {
                            resp.Duration = "0h";
                        }
                    }
                }

                var result = new PaginatedCourseResponse
                {
                    Courses = courseResponses,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = request.PageNumber
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<Guid> GetCourseAuthorIdAsync(Guid courseId)
        {
            var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId);
            return course?.CreatedBy ?? Guid.Empty;
        }

        public async Task<ApiResponse> GetInstructorMetricsAsync()
        {
            var response = new ApiResponse();
            try
            {
                var userId = _claimService.GetUserClaim().UserId;

                var enrollments = await _unitOfWork.Enrollments.GetAllAsync(
                    e => e.Course.CreatedBy == userId && !e.IsDeleted && (e.Status == 1 || e.Status == 2),
                    include: e => e.Include(x => x.Course)
                );

                var totalStudents = enrollments.Select(e => e.UserId).Distinct().Count();
                var totalEnrollments = enrollments.Count();
                var totalRevenue = enrollments.Sum(e => e.Course.Price);

                var activeCoursesCount = await _unitOfWork.Courses.CountAsync(c => c.CreatedBy == userId && c.Status == 2 && !c.IsDeleted);

                var instructorCourses = await _unitOfWork.Courses.GetAllAsync(c => c.CreatedBy == userId && !c.IsDeleted);
                var courseIds = instructorCourses.Select(c => c.CourseId).ToList();

                var reviews = await _unitOfWork.CourseReviews.GetAllAsync(r => courseIds.Contains(r.CourseId) && !r.IsDeleted);
                double avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 5.0;

                var popularCourses = enrollments
                    .GroupBy(e => new { e.CourseId, e.Course.Title })
                    .Select(g => new
                    {
                        Title = g.Key.Title,
                        Enrollments = g.Count(),
                        Revenue = g.Sum(e => e.Course.Price)
                    })
                    .OrderByDescending(x => x.Enrollments)
                    .Take(5)
                    .ToList();

                var today = DateTime.UtcNow;
                var sixMonthsAgo = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

                var recentEnrollments = enrollments
                    .Where(e => e.EnrolledAt.HasValue && e.EnrolledAt.Value >= sixMonthsAgo && e.EnrolledAt.Value <= today)
                    .ToList();

                var monthlyRevenue = Enumerable.Range(0, 6)
                    .Select(i => sixMonthsAgo.AddMonths(i))
                    .Select(m => new
                    {
                        Month = $"{m.Month}/{m.Year.ToString().Substring(2)}",
                        Amount = recentEnrollments.Where(e => e.EnrolledAt.HasValue && e.EnrolledAt.Value.Month == m.Month && e.EnrolledAt.Value.Year == m.Year).Sum(e => e.Course.Price)
                    })
                    .ToList();

                return response.SetOk(new
                {
                    TotalStudents = totalStudents,
                    TotalEnrollments = totalEnrollments,
                    TotalRevenue = totalRevenue,
                    ActiveCoursesCount = activeCoursesCount,
                    AverageRating = Math.Round(avgRating, 1),
                    PopularCourses = popularCourses,
                    MonthlyRevenue = monthlyRevenue
                });
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseForLearningAsync(Guid courseId)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && c.Status == 2 && !c.IsDeleted);
                if (course == null) return response.SetNotFound("Khóa học không tồn tại hoặc chưa được publish.");

                var courseDto = _mapper.Map<CourseEditSummaryResponse>(course);

                var modulesData = await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted);
                var modules = modulesData != null ? modulesData.ToList() : new List<Module>();
                var moduleIds = modules.Select(m => m.ModuleId).ToList();

                var lessons = new List<Lesson>();
                var lessonItems = new List<LessonItem>();
                var resources = new List<LessonResource>();
                var gradedItems = new List<GradedItem>();
                var questions = new List<Question>();
                var answerOptions = new List<AnswerOption>();

                if (moduleIds.Any())
                {
                    var lessonsData = await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted);
                    lessons = lessonsData != null ? lessonsData.ToList() : new List<Lesson>();
                    var lessonIds = lessons.Select(l => l.LessonId).ToList();

                    if (lessonIds.Any())
                    {
                        var itemsData = await _unitOfWork.LessonItems.GetAllAsync(li => lessonIds.Contains(li.LessonId) && !li.IsDeleted);
                        lessonItems = itemsData != null ? itemsData.ToList() : new List<LessonItem>();
                        var itemIds = lessonItems.Select(li => li.LessonItemId).ToList();

                        if (itemIds.Any())
                        {
                            var resData = await _unitOfWork.LessonResources.GetAllAsync(r => itemIds.Contains(r.LessonItemId) && !r.IsDeleted);
                            resources = resData != null ? resData.ToList() : new List<LessonResource>();

                            var gradedData = await _unitOfWork.GradedItems.GetAllAsync(g => itemIds.Contains(g.LessonItemId) && !g.IsDeleted);
                            gradedItems = gradedData != null ? gradedData.ToList() : new List<GradedItem>();
                            var gradedItemIds = gradedItems.Select(g => g.GradedItemId).ToList();

                            if (gradedItemIds.Any())
                            {
                                var qData = await _unitOfWork.Questions.GetAllAsync(q => gradedItemIds.Contains(q.GradedItemId) && !q.IsDeleted);
                                questions = qData != null ? qData.ToList() : new List<Question>();
                                var questionIds = questions.Select(q => q.QuestionId).ToList();

                                if (questionIds.Any())
                                {
                                    var ansData = await _unitOfWork.AnswerOptions.GetAllAsync(a => questionIds.Contains(a.QuestionId) && !a.IsDeleted);
                                    answerOptions = ansData != null ? ansData.ToList() : new List<AnswerOption>();
                                }
                            }
                        }
                    }
                }

                var bundle = new CourseEditBundleResponse
                {
                    Course = courseDto,
                    Modules = _mapper.Map<List<CourseModuleEditResponse>>(modules),
                    Lessons = _mapper.Map<List<CourseLessonEditResponse>>(lessons),
                    LessonItems = _mapper.Map<List<CourseLessonItemEditResponse>>(lessonItems),
                    LessonResources = _mapper.Map<List<CourseLessonResourceEditResponse>>(resources),
                    GradedItems = gradedItems.Select(item => new CourseGradedItemEditResponse
                    {
                        GradedItemId = item.GradedItemId,
                        LessonItemId = item.LessonItemId,
                        SubmissionGuidelines = item.SubmissionGuidelines
                    }).ToList(),
                    Questions = questions.Select(q => new CourseQuestionEditResponse
                    {
                        QuestionId = q.QuestionId,
                        GradedItemId = q.GradedItemId,
                        Content = q.Content,
                        OrderIndex = q.OrderIndex,
                        Points = q.Points
                    }).ToList(),
                    AnswerOptions = _mapper.Map<List<CourseAnswerOptionEditResponse>>(answerOptions)
                };

                return response.SetOk(bundle);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetEnrolledCoursesForStudentAsync()
        {
            ApiResponse response = new ApiResponse();

            try
            {
                var studentId = _service.GetUserClaim().UserId;
                var enrollments = await _unitOfWork.Enrollments
                    .GetAllAsync(e => e.UserId == studentId);

                var courseIds = enrollments
                    .Select(e => e.CourseId)
                    .Distinct()
                    .ToList();

                var courses = await _unitOfWork.Courses
                    .GetAllAsync(c => courseIds.Contains(c.CourseId));

                var result = _mapper.Map<List<CourseResponse>>(courses);
                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> ApproveCourseAsync(ApproveCourseRequest request)
        {
            ApiResponse response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                if (claim.Role != 0)
                {
                    return response.SetBadRequest(message: "Chỉ Admin có quyền duyệt khóa học");
                }

                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == request.CourseId && !c.IsDeleted);
                if (course == null) return response.SetNotFound("Course not found");
                if (course.Status != 1) return response.SetBadRequest(message: "Chỉ có thể duyệt/từ chối khóa học ở trạng thái Pending");

                var instructor = await _unitOfWork.Users.GetAsync(u => u.UserId == course.CreatedBy);
                var adminId = claim.UserId;

                if (!request.Status)
                {
                    // Reject → Draft
                    if (string.IsNullOrEmpty(request.RejectReason))
                        return response.SetBadRequest("Reject reason is required");

                    course.Status = 0; // Back to Draft
                    course.RejectReason = request.RejectReason;
                    course.RejectedAt = DateTime.UtcNow;
                    course.UpdatedAt = DateTime.UtcNow;
                    course.UpdatedBy = adminId;

                    await _unitOfWork.BeginTransactionAsync();
                    _unitOfWork.Courses.Update(course);
                    await _unitOfWork.CommitAsync();

                    if (instructor != null)
                    {
                        await _emailService.SendRejectCourseEmail(instructor.FullName, instructor.Email, request.RejectReason, course.Title);
                    }

                    return response.SetOk(new {
                        Message = "Course rejected & email sent",
                        InstructorId = course.CreatedBy.ToString(),
                        CourseTitle = course.Title,
                        Status = "Rejected",
                        Reason = request.RejectReason
                    });
                }
                else
                {
                    // Approve → Published
                    course.Status = 2;
                    course.RejectReason = null;
                    course.PublishedAt = DateTime.UtcNow;
                    course.UpdatedAt = DateTime.UtcNow;
                    course.UpdatedBy = adminId;

                    await _unitOfWork.BeginTransactionAsync();
                    _unitOfWork.Courses.Update(course);
                    await _unitOfWork.CommitAsync();

                    if (instructor != null)
                    {
                        await _emailService.SendApproveCourseEmail(
                            receiverName: instructor.FullName,
                            receiverEmail: instructor.Email,
                            courseTitle: course.Title
                        );
                    }

                    return response.SetOk(new {
                        Message = "Course approved & email sent",
                        InstructorId = course.CreatedBy.ToString(),
                        CourseTitle = course.Title,
                        Status = "Approved"
                    });
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> SubmitCourseForReviewAsync(Guid courseId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null) return response.SetNotFound("Course not found");
                if (course.CreatedBy != claim.UserId)
                    return response.SetBadRequest(message: "Bạn không có quyền submit khóa học này");
                if (course.Status != 0)
                    return response.SetBadRequest(message: "Chỉ có thể submit khóa học ở trạng thái Draft");

                await _unitOfWork.BeginTransactionAsync();
                course.Status = 1;
                course.SubmittedAt = DateTime.UtcNow;
                course.UpdatedAt = DateTime.UtcNow;
                course.UpdatedBy = claim.UserId;
                _unitOfWork.Courses.Update(course);

                await _unitOfWork.CommitAsync();
                return response.SetOk("Course submitted for review.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCoursesByStatusAsync(int status)
        {
            var response = new ApiResponse();
            try
            {
                var courses = await _unitOfWork.Courses.GetAllAsync(c => c.Status == status && !c.IsDeleted);
                if (status == 2)
                {
                    var courseResponses = _mapper.Map<List<CourseResponse>>(courses);
                    return response.SetOk(courseResponses);
                }
                return response.SetOk(courses.ToList());
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseByIdAsync(Guid courseId)
        {
            var response = new ApiResponse();
            try
            {
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId);
                if (course == null) return response.SetNotFound("Course not found");

                var courseResponse = _mapper.Map<CourseResponse>(course);
                return response.SetOk(courseResponse);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseDetailForStudentAsync(Guid courseId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                if (claim.Role != 2)
                {
                    return response.SetBadRequest(message: "Only students can access the learning page.");
                }

                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null)
                {
                    return response.SetNotFound("Course not found");
                }

                if (course.Status != 2)
                {
                    return response.SetBadRequest(message: "This course is not published yet.");
                }

                var enrollment = await _unitOfWork.Enrollments.GetAsync(e =>
                    e.UserId == claim.UserId
                    && e.CourseId == courseId
                    && (e.Status == 1 || e.Status == 2)
                    && !e.IsDeleted);

                if (enrollment == null)
                {
                    return response.SetBadRequest(message: "You are not enrolled in this course.");
                }

                var hasSuccessfulPayment = await _unitOfWork.Payments.AnyAsync(p =>
                    p.UserId == claim.UserId
                    && p.CourseId == courseId
                    && p.Status == 1
                    && !p.IsDeleted);

                if (course.Price > 0 && !hasSuccessfulPayment)
                {
                    return response.SetBadRequest(message: "Payment is required before accessing this course.");
                }

                var modules = (await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted))
                    .OrderBy(m => m.Index)
                    .ToList();
                var moduleIds = modules.Select(m => m.ModuleId).ToList();

                var lessons = (await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted))
                    .OrderBy(l => l.OrderIndex)
                    .ToList();
                var lessonIds = lessons.Select(l => l.LessonId).ToList();

                var lessonItems = (await _unitOfWork.LessonItems.GetAllAsync(li => lessonIds.Contains(li.LessonId) && !li.IsDeleted))
                    .OrderBy(li => li.OrderIndex)
                    .ToList();
                var lessonItemIds = lessonItems.Select(li => li.LessonItemId).ToList();

                var lessonResources = (await _unitOfWork.LessonResources.GetAllAsync(lr => lessonItemIds.Contains(lr.LessonItemId) && !lr.IsDeleted))
                    .OrderBy(lr => lr.OrderIndex)
                    .ToList();

                var gradedItems = await _unitOfWork.GradedItems.GetAllAsync(gi => lessonItemIds.Contains(gi.LessonItemId) && !gi.IsDeleted);
                var gradedItemIds = gradedItems.Select(gi => gi.GradedItemId).ToList();

                var studentAttempts = await _unitOfWork.GradedAttempts.GetAllAsync(ga =>
                    ga.UserId == claim.UserId
                    && gradedItemIds.Contains(ga.GradedItemId)
                    && !ga.IsDeleted);

                var questions = (await _unitOfWork.Questions.GetAllAsync(q => gradedItemIds.Contains(q.GradedItemId) && !q.IsDeleted))
                    .OrderBy(q => q.OrderIndex)
                    .ToList();
                var questionIds = questions.Select(q => q.QuestionId).ToList();

                var answerOptions = (await _unitOfWork.AnswerOptions.GetAllAsync(ao => questionIds.Contains(ao.QuestionId) && !ao.IsDeleted))
                    .OrderBy(ao => ao.OrderIndex)
                    .ToList();

                var progressRows = await _unitOfWork.UserLessonProgresses.GetAllAsync(p =>
                    p.UserId == claim.UserId
                    && lessonIds.Contains(p.LessonId));

                // Auto-healing check: if any lesson progress record is missing, initialize it
                var existingLessonIds = progressRows.Select(p => p.LessonId).ToHashSet();
                var missingLessonIds = lessonIds.Where(id => !existingLessonIds.Contains(id)).ToList();
                if (missingLessonIds.Any())
                {
                    var newProgresses = new List<UserLessonProgress>();
                    foreach (var missingId in missingLessonIds)
                    {
                        var newProg = new UserLessonProgress
                        {
                            LessonProgressId = Guid.NewGuid(),
                            UserId = claim.UserId,
                            LessonId = missingId,
                            IsCompleted = false,
                            CompletionPercent = 0,
                            LastWatchedSecond = 0,
                            LastAccessedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.UserLessonProgresses.AddAsync(newProg);
                        newProgresses.Add(newProg);
                    }
                    await _unitOfWork.SaveChangeAsync();
                    progressRows.AddRange(newProgresses);
                }

                var totalLessons = lessonIds.Count;
                var completedLessons = progressRows.Count(p => p.IsCompleted);
                decimal percent = totalLessons > 0 ? ((decimal)completedLessons / totalLessons) * 100m : 0m;
                decimal roundedPercent = Math.Round(percent, 2);

                if (enrollment.ProgressPercent != roundedPercent)
                {
                    enrollment.ProgressPercent = roundedPercent;
                    _unitOfWork.Enrollments.Update(enrollment);
                    await _unitOfWork.SaveChangeAsync();
                }

                var progressByLesson = progressRows
                    .GroupBy(p => p.LessonId)
                    .ToDictionary(g => g.Key, g => g.Any(x => x.IsCompleted));
                var gradedByLessonItem = gradedItems.ToDictionary(g => g.LessonItemId, g => g);

                var result = new StudentLearningDetailResponse
                {
                    CourseId = course.CourseId,
                    Title = course.Title,
                    Description = course.Description,
                    ProgressPercent = enrollment.ProgressPercent,
                    Modules = modules.Select(module => new StudentLearningModuleResponse
                    {
                        ModuleId = module.ModuleId,
                        Title = module.Name,
                        OrderIndex = module.Index,
                        Lessons = lessons
                            .Where(lesson => lesson.ModuleId == module.ModuleId)
                            .OrderBy(lesson => lesson.OrderIndex)
                            .Select(lesson => new StudentLearningLessonResponse
                            {
                                LessonId = lesson.LessonId,
                                Title = lesson.Title,
                                Description = lesson.Description,
                                OrderIndex = lesson.OrderIndex,
                                EstimatedMinutes = lesson.EstimatedMinutes,
                                IsCompleted = progressByLesson.TryGetValue(lesson.LessonId, out var isCompleted) && isCompleted,
                                Materials = lessonItems
                                    .Where(item => item.LessonId == lesson.LessonId)
                                    .OrderBy(item => item.OrderIndex)
                                    .Select(item =>
                                    {
                                        var firstResource = lessonResources
                                            .Where(resource => resource.LessonItemId == item.LessonItemId)
                                            .OrderBy(resource => resource.OrderIndex)
                                            .FirstOrDefault();

                                        var material = new StudentLearningMaterialResponse
                                        {
                                            LessonItemId = item.LessonItemId,
                                            Type = item.Type,
                                            OrderIndex = item.OrderIndex,
                                            Title = firstResource?.Title ?? GetMaterialTypeLabel(item.Type),
                                            Description = lesson.Description,
                                            VideoUrl = firstResource?.ResourceUrl,
                                            Content = firstResource?.TextContent
                                        };

                                        if (item.Type == 2 && gradedByLessonItem.TryGetValue(item.LessonItemId, out var gradedItem))
                                        {
                                            var quizQuestions = questions
                                                .Where(question => question.GradedItemId == gradedItem.GradedItemId)
                                                .OrderBy(question => question.OrderIndex)
                                                .Select(question => new StudentLearningQuestionResponse
                                                {
                                                    QuestionId = question.QuestionId,
                                                    Content = question.Content,
                                                    OrderIndex = question.OrderIndex,
                                                    Options = answerOptions
                                                        .Where(option => option.QuestionId == question.QuestionId)
                                                        .OrderBy(option => option.OrderIndex)
                                                        .Select(option => new StudentLearningAnswerOptionResponse
                                                        {
                                                            AnswerOptionId = option.AnswerOptionId,
                                                            Text = option.Text,
                                                            OrderIndex = option.OrderIndex
                                                        })
                                                        .ToList()
                                                })
                                                .ToList();

                                            material.Quiz = new StudentLearningQuizResponse
                                            {
                                                GradedItemId = gradedItem.GradedItemId,
                                                Title = firstResource?.Title ?? "Quiz",
                                                Questions = quizQuestions
                                            };
                                        }

                                        if (item.Type == 3 && gradedByLessonItem.TryGetValue(item.LessonItemId, out var practiceItem))
                                        {
                                            var attempts = studentAttempts
                                                .Where(a => a.GradedItemId == practiceItem.GradedItemId)
                                                .OrderByDescending(a => a.AttemptNumber)
                                                .Select(a => new StudentLearningPracticeAttemptResponse
                                                {
                                                    GradedAttemptId = a.GradedAttemptId,
                                                    AttemptNumber = a.AttemptNumber,
                                                    Status = a.Status,
                                                    SubmittedAt = a.SubmittedAt,
                                                    Score = a.Score,
                                                    IsPassed = a.IsPassed,
                                                    SubmittedText = a.SubmittedText,
                                                    Feedback = a.Feedback
                                                })
                                                .ToList();

                                            var promptText = practiceItem.SubmissionGuidelines;
                                            var firstQ = questions.FirstOrDefault(q => q.GradedItemId == practiceItem.GradedItemId);
                                            if (firstQ != null && (string.IsNullOrEmpty(promptText) || promptText == "Write your essay based on the prompt. AI will evaluate it."))
                                            {
                                                promptText = firstQ.Content;
                                            }

                                            material.Practice = new StudentLearningPracticeResponse
                                            {
                                                GradedItemId = practiceItem.GradedItemId,
                                                SubmissionGuidelines = promptText,
                                                MaxScore = practiceItem.MaxScore,
                                                Attempts = attempts
                                            };
                                        }

                                        return material;
                                    })
                                    .ToList()
                            })
                            .ToList()
                    }).ToList()
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetCourseForEditAsync(Guid courseId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null) return response.SetNotFound(message: "Không tìm thấy khóa học");

                // Permission: owner (instructor) or admin (review)
                if (course.CreatedBy != claim.UserId && claim.Role != 0)
                    return response.SetBadRequest(message: "Bạn không có quyền chỉnh sửa khóa học này");

                // Load modules + lessons + lesson items + resources + graded items + questions + answer options
                var modules = (await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted))
                    .OrderBy(m => m.Index).ToList();
                var moduleIds = modules.Select(m => m.ModuleId).ToList();

                var lessons = (await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted))
                    .OrderBy(l => l.OrderIndex).ToList();
                var lessonIds = lessons.Select(l => l.LessonId).ToList();

                var lessonItems = (await _unitOfWork.LessonItems.GetAllAsync(li => lessonIds.Contains(li.LessonId) && !li.IsDeleted))
                    .OrderBy(li => li.OrderIndex).ToList();
                var lessonItemIds = lessonItems.Select(li => li.LessonItemId).ToList();

                var lessonResources = (await _unitOfWork.LessonResources.GetAllAsync(lr => lessonItemIds.Contains(lr.LessonItemId) && !lr.IsDeleted)).ToList();
                var gradedItems = (await _unitOfWork.GradedItems.GetAllAsync(gi => lessonItemIds.Contains(gi.LessonItemId) && !gi.IsDeleted)).ToList();

                var gradedItemIds = gradedItems.Select(gi => gi.GradedItemId).ToList();
                var questions = (await _unitOfWork.Questions.GetAllAsync(q => gradedItemIds.Contains(q.GradedItemId) && !q.IsDeleted))
                    .OrderBy(q => q.OrderIndex).ToList();
                var questionIds = questions.Select(q => q.QuestionId).ToList();
                var answerOptions = (await _unitOfWork.AnswerOptions.GetAllAsync(ao => questionIds.Contains(ao.QuestionId) && !ao.IsDeleted))
                    .OrderBy(ao => ao.OrderIndex).ToList();

                var result = new CourseEditBundleResponse
                {
                    Course = new CourseEditSummaryResponse
                    {
                        CourseId = course.CourseId,
                        LanguageId = course.LanguageId,
                        Title = course.Title,
                        Subtitle = course.Subtitle,
                        Description = course.Description,
                        Image = course.Image,
                        Status = course.Status,
                        Price = course.Price,
                        Level = course.Level,
                        Tags = course.Tags,
                        SubmittedAt = course.SubmittedAt
                    },
                    Modules = modules.Select(module => new CourseModuleEditResponse
                    {
                        ModuleId = module.ModuleId,
                        CourseId = module.CourseId,
                        Name = module.Name,
                        Description = module.Description,
                        Index = module.Index
                    }).ToList(),
                    Lessons = lessons.Select(lesson => new CourseLessonEditResponse
                    {
                        LessonId = lesson.LessonId,
                        ModuleId = lesson.ModuleId,
                        Title = lesson.Title,
                        Description = lesson.Description,
                        EstimatedMinutes = lesson.EstimatedMinutes,
                        OrderIndex = lesson.OrderIndex
                    }).ToList(),
                    LessonItems = lessonItems.Select(item => new CourseLessonItemEditResponse
                    {
                        LessonItemId = item.LessonItemId,
                        LessonId = item.LessonId,
                        Type = item.Type,
                        OrderIndex = item.OrderIndex
                    }).ToList(),
                    LessonResources = lessonResources.Select(resource => new CourseLessonResourceEditResponse
                    {
                        LessonResourceId = resource.LessonResourceId,
                        LessonItemId = resource.LessonItemId,
                        Title = resource.Title,
                        ResourceType = resource.ResourceType,
                        ResourceUrl = resource.ResourceUrl,
                        TextContent = resource.TextContent,
                        VideoSourceType = resource.VideoSourceType,
                        OrderIndex = resource.OrderIndex
                    }).ToList(),
                    GradedItems = gradedItems.Select(item => new CourseGradedItemEditResponse
                    {
                        GradedItemId = item.GradedItemId,
                        LessonItemId = item.LessonItemId,
                        SubmissionGuidelines = item.SubmissionGuidelines
                    }).ToList(),
                    Questions = questions.Select(question => new CourseQuestionEditResponse
                    {
                        QuestionId = question.QuestionId,
                        GradedItemId = question.GradedItemId,
                        Content = question.Content,
                        OrderIndex = question.OrderIndex,
                        Points = question.Points
                    }).ToList(),
                    AnswerOptions = answerOptions.Select(option => new CourseAnswerOptionEditResponse
                    {
                        AnswerOptionId = option.AnswerOptionId,
                        QuestionId = option.QuestionId,
                        Text = option.Text,
                        IsCorrect = option.IsCorrect,
                        OrderIndex = option.OrderIndex
                    }).ToList()
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> ValidateAndSubmitForReviewAsync(Guid courseId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                var course = await _unitOfWork.Courses.GetAsync(c => c.CourseId == courseId && !c.IsDeleted);
                if (course == null) return response.SetNotFound(message: "Không tìm thấy khóa học");
                if (course.CreatedBy != claim.UserId)
                    return response.SetBadRequest(message: "Bạn không có quyền submit khóa học này");
                if (course.Status != 0)
                    return response.SetBadRequest(message: "Chỉ có thể submit khóa học ở trạng thái Draft");

                // Validate: at least 1 lesson with at least 1 material
                var modules = await _unitOfWork.Modules.GetAllAsync(m => m.CourseId == courseId && !m.IsDeleted);
                var moduleIds = modules.Select(m => m.ModuleId).ToList();
                var lessons = await _unitOfWork.Lessons.GetAllAsync(l => moduleIds.Contains(l.ModuleId) && !l.IsDeleted);

                if (!lessons.Any())
                    return response.SetBadRequest(message: "Khóa học cần ít nhất 1 bài học trước khi submit");

                var lessonIds = lessons.Select(l => l.LessonId).ToList();
                var lessonItems = await _unitOfWork.LessonItems.GetAllAsync(li => lessonIds.Contains(li.LessonId) && !li.IsDeleted);

                foreach (var lesson in lessons)
                {
                    var itemsForLesson = lessonItems.Where(li => li.LessonId == lesson.LessonId).ToList();
                    if (!itemsForLesson.Any())
                        return response.SetBadRequest(message: $"Bài học '{lesson.Title}' cần ít nhất 1 tài liệu (material)");
                }

                // Transition Draft → Pending
                await _unitOfWork.BeginTransactionAsync();
                course.Status = 1; // Pending
                course.SubmittedAt = DateTime.UtcNow;
                course.UpdatedAt = DateTime.UtcNow;
                course.RejectReason = null;
                _unitOfWork.Courses.Update(course);
                await _unitOfWork.CommitAsync();

                return response.SetOk("Khóa học đã được gửi để duyệt thành công!");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetPendingCoursesForAdminAsync()
        {
            var response = new ApiResponse();
            try
            {
                var claim = _service.GetUserClaim();
                if (claim.Role != 0)
                {
                    return response.SetBadRequest(message: "Chỉ Admin có quyền xem danh sách chờ duyệt");
                }

                var courses = await _unitOfWork.Courses.GetAllAsync(c => !c.IsDeleted && c.Status == 1);
                var result = courses
                    .OrderByDescending(c => c.SubmittedAt)
                    .Select(c => new PendingCourseReviewResponse
                    {
                        CourseId = c.CourseId,
                        Title = c.Title,
                        Subtitle = c.Subtitle,
                        Image = c.Image,
                        Level = c.Level,
                        Price = c.Price,
                        SubmittedAt = c.SubmittedAt
                    })
                    .ToList();

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        public async Task<ApiResponse> GetLandingStatsAsync()
        {
            var response = new ApiResponse();
            try
            {
                int studentsCount = await _unitOfWork.Users.CountAsync(u => u.Role == 2 && !u.IsDeleted);
                int displayStudents = studentsCount + 4820;

                int instructorsCount = await _unitOfWork.Users.CountAsync(u => u.Role == 1 && !u.IsDeleted);
                int displayInstructors = instructorsCount > 0 ? instructorsCount : 12;

                double avgRating = 4.9;
                int reviewCount = await _unitOfWork.CourseReviews.CountAsync(r => !r.IsDeleted);
                if (reviewCount > 0)
                {
                    var reviews = await _unitOfWork.CourseReviews.GetAllAsync(r => !r.IsDeleted);
                    avgRating = Math.Round(reviews.Average(r => r.Rating), 1);
                }

                var stats = new
                {
                    studentsCount = displayStudents,
                    instructorsCount = displayInstructors,
                    averageRating = avgRating
                };

                return response.SetOk(stats);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }

        private static string GetMaterialTypeLabel(int type)
        {
            return type switch
            {
                0 => "Video",
                1 => "Reading",
                2 => "Quiz",
                _ => "Material"
            };
        }
    }
}
