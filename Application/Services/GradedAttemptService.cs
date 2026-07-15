using OnlineLearningPlatformApi.Application.IServices;
using AutoMapper;
using OnlineLearningPlatformApi.Domain.Entities;
using OnlineLearningPlatformApi.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;



namespace OnlineLearningPlatformApi.Application.Services
{
    public class GradedAttemptService : IGradedAttemptService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimService _claimService;
        private readonly IMapper _mapper;
        private readonly IFirebaseStorageService _storage;
        private readonly IEmailService _emailService;

        public GradedAttemptService(IUnitOfWork unitOfWork, IClaimService claimService, IMapper mapper, IFirebaseStorageService storage, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _claimService = claimService;
            _mapper = mapper;
            _storage = storage;
            _emailService = emailService;
        }

        public async Task<ApiResponse> StartAttemptAsync(Guid gradedItemId)
        {
            ApiResponse response = new ApiResponse();
            var userId = _claimService.GetUserClaim().UserId;

            var gradedItem = await _unitOfWork.GradedItems
                .GetAsync(x => x.GradedItemId == gradedItemId);

            if (gradedItem == null)
                return response.SetNotFound("Graded item not found");

            var attemptCount = await _unitOfWork.GradedAttempts.CountAsync(
                x => x.UserId == userId && x.GradedItemId == gradedItemId);

            var attempt = new GradedAttempt
            {
                GradedAttemptId = Guid.NewGuid(),
                UserId = userId,
                GradedItemId = gradedItemId,
                AttemptNumber = attemptCount + 1,
                Status = 0,
                SubmittedAt = DateTime.UtcNow
            };

            await _unitOfWork.GradedAttempts.AddAsync(attempt);
            await _unitOfWork.SaveChangeAsync();

            return response.SetOk(attempt.GradedAttemptId);
        }
        public async Task<ApiResponse> SubmitShortAnswerAsync(Guid attemptId, Guid questionId, string answer, IFormFile? file)
        {
            ApiResponse response = new ApiResponse();
            var claim = _claimService.GetUserClaim().UserId;
            var student = await _unitOfWork.Users.GetAsync(x => x.UserId == claim);
            var attempt = await _unitOfWork.GradedAttempts
                .GetAsync(x => x.GradedAttemptId == attemptId);

            if (attempt == null)
                return response.SetNotFound("Attempt not found");

            if (attempt.Status != 0)
                return response.SetBadRequest("Attempt already submitted");

            var submission = new QuestionSubmission
            {
                QuestionSubmissionId = Guid.NewGuid(),
                GradedAttemptId = attemptId,
                QuestionId = questionId,
                AnswerText = answer,
            };

            await _unitOfWork.QuestionSubmissions.AddAsync(submission);
            await _unitOfWork.SaveChangeAsync();
            var instructor = await _unitOfWork.Users.GetAsync(x => x.UserId == attempt.GradedAttemptNavigation.LessonItem.Lesson.Module.Course.CreatedBy);
            if (instructor != null)
            {
                await _emailService.SendShortAnswerNotifyToInstructor(
        instructorName: instructor.FullName,
        instructorEmail: instructor.Email,
        studentName: student.FullName,
        courseTitle: attempt.GradedAttemptNavigation.LessonItem.Lesson.Module.Course.Title
    );
            }

            return response.SetOk("Answer submitted");
        }

        public async Task<ApiResponse> SubmitAttemptAsync(Guid attemptId)
        {
            ApiResponse response = new ApiResponse();

            var attempt = await _unitOfWork.GradedAttempts.GetAsync(
                x => x.GradedAttemptId == attemptId,
                include: q => q
                    .Include(a => a.GradedAttemptNavigation)
                        .ThenInclude(g => g.LessonItem)
                            .ThenInclude(l => l.Lesson)
                                .ThenInclude(m => m.Module)
                    .Include(a => a.QuestionSubmissions)
                        .ThenInclude(q => q.Question)
                            .ThenInclude(q => q.AnswerOptions));

            if (attempt == null)
                return response.SetNotFound("Attempt not found");

            attempt.Status = 1;
            attempt.SubmittedAt = DateTime.UtcNow;
            bool hasManualQuestion = false;

            //Auto Graded
            if (attempt.GradedAttemptNavigation!.IsAutoGraded)
            {
                decimal totalScore = 0;

                foreach (var submission in attempt.QuestionSubmissions!)
                {
                    var question = submission.Question!;

                    // ❌ ShortAnswer → để instructor chấm
                    if (question.Type == 2)
                    {
                        hasManualQuestion = true;
                        continue;
                    }

                    // ✅ MultipleChoice / TrueFalse
                    var correctAnswerIds = question.AnswerOptions!
                        .Where(x => x.IsCorrect)
                        .Select(a => a.AnswerOptionId)
                        .ToHashSet();
                    //Dap an student chon
                    var selectedAnswerIds = submission.SubmissionAnswerOptions!
                        .Select(s => s.AnswerOptionId)
                        .ToHashSet();
                    bool isCorrect = selectedAnswerIds.SetEquals(correctAnswerIds);
                    if (isCorrect)
                    {
                        submission.Score = question.Points;
                        totalScore += question.Points;
                    }
                    else
                    {
                        submission.Score = 0;
                    }
                }

                // 🎯 Chỉ auto-grade hoàn toàn khi KHÔNG có câu tự luận
                if (!hasManualQuestion)
                {
                    attempt.Score = totalScore;
                    attempt.Status = 2;
                    attempt.GradedAt = DateTime.UtcNow;

                    await UpdateLessonAndEnrollmentProgress(attempt);
                }
            }

            _unitOfWork.GradedAttempts.Update(attempt);
            await _unitOfWork.SaveChangeAsync();

            return response.SetOk(
                hasManualQuestion
                    ? "Attempt submitted, waiting for instructor grading"
                    : "Attempt auto graded successfully");
        }

        public async Task<ApiResponse> GradeAssignmentAsync(Guid attemptId, decimal score)
        {
            ApiResponse response = new ApiResponse();

            var attempt = await _unitOfWork.GradedAttempts.GetAsync(
                x => x.GradedAttemptId == attemptId,
                include: q => q
                    .Include(a => a.GradedAttemptNavigation)
                        .ThenInclude(g => g.LessonItem)
                            .ThenInclude(k => k.Lesson)
                                .ThenInclude(l => l.Module));

            if (attempt == null)
                return response.SetNotFound("Attempt not found");

            attempt.Score = score;
            attempt.Status = 3;
            attempt.GradedAt = DateTime.UtcNow;

            await UpdateLessonAndEnrollmentProgress(attempt);

            _unitOfWork.GradedAttempts.Update(attempt);
            await _unitOfWork.SaveChangeAsync();

            return response.SetOk("Assignment graded");
        }

        private async Task UpdateLessonAndEnrollmentProgress(GradedAttempt attempt)
        {
            var userId = attempt.UserId;
            var lessonId = attempt.GradedAttemptNavigation!.LessonItem.Lesson.LessonId;
            var courseId = attempt.GradedAttemptNavigation.LessonItem.Lesson.Module!.CourseId;

            // LessonProgress
            var lessonProgress = await _unitOfWork.UserLessonProgresses.GetAsync(
                x => x.UserId == userId && x.LessonId == lessonId);

            if (lessonProgress != null)
            {
                lessonProgress.IsCompleted = true;
                lessonProgress.CompletionPercent = 100;
                lessonProgress.CompletedAt = DateTime.UtcNow;

                _unitOfWork.UserLessonProgresses.Update(lessonProgress);
            }

            // Enrollment Progress
            var totalLessons = await _unitOfWork.Lessons.CountAsync(
                x => x.Module!.CourseId == courseId);

            var completedLessons = await _unitOfWork.UserLessonProgresses.CountAsync(
                x => x.UserId == userId && x.IsCompleted);

            var enrollment = await _unitOfWork.Enrollments.GetAsync(
                x => x.UserId == userId && x.CourseId == courseId);

            if (enrollment != null)
            {
                enrollment.ProgressPercent =
                    Math.Round(completedLessons * 100m / totalLessons, 2);

                if (enrollment.ProgressPercent == 100)
                {
                    enrollment.Status = 2;
                    enrollment.CompletedAt = DateTime.UtcNow;
                }

            }
        }

        public async Task<ApiResponse> SubmitPracticeAttemptAsync(OnlineLearningPlatformApi.Application.Requests.Practice.SubmitPracticeAttemptRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var userId = _claimService.GetUserClaim().UserId;

                var gradedItem = await _unitOfWork.GradedItems.GetAsync(x => x.LessonItemId == request.LessonItemId && !x.IsDeleted);
                if (gradedItem == null)
                {
                    // Create GradedItem on the fly if not exists
                    gradedItem = new GradedItem
                    {
                        GradedItemId = Guid.NewGuid(),
                        LessonItemId = request.LessonItemId,
                        MaxScore = 100,
                        IsAutoGraded = false,
                        GradedItemType = 1,
                        SubmissionGuidelines = "Bài tập thực hành",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    await _unitOfWork.GradedItems.AddAsync(gradedItem);
                    await _unitOfWork.SaveChangeAsync();
                }

                var attemptCount = await _unitOfWork.GradedAttempts.CountAsync(
                    x => x.UserId == userId && x.GradedItemId == gradedItem.GradedItemId && !x.IsDeleted);

                var attempt = new GradedAttempt
                {
                    GradedAttemptId = Guid.NewGuid(),
                    UserId = userId,
                    GradedItemId = gradedItem.GradedItemId,
                    AttemptNumber = attemptCount + 1,
                    Status = 2, // 2 = Graded
                    SubmittedAt = DateTime.UtcNow,
                    GradedAt = DateTime.UtcNow,
                    Score = request.Score,
                    MaxScore = 100,
                    IsPassed = request.IsPassed,
                    SubmittedText = request.SubmittedText,
                    Feedback = request.Feedback,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                await _unitOfWork.GradedAttempts.AddAsync(attempt);
                await _unitOfWork.SaveChangeAsync();

                var dbAttempt = await _unitOfWork.GradedAttempts.GetAsync(
                    x => x.GradedAttemptId == attempt.GradedAttemptId,
                    include: q => q
                        .Include(a => a.GradedAttemptNavigation)
                            .ThenInclude(g => g.LessonItem)
                                .ThenInclude(l => l.Lesson)
                                    .ThenInclude(m => m.Module));

                if (dbAttempt != null)
                {
                    await UpdateLessonAndEnrollmentProgress(dbAttempt);
                    await _unitOfWork.SaveChangeAsync();
                }

                return response.SetOk(new
                {
                    gradedAttemptId = attempt.GradedAttemptId,
                    score = attempt.Score,
                    feedback = attempt.Feedback,
                    isPassed = attempt.IsPassed
                });
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(message: ex.Message);
            }
        }
    }
}
