using Microsoft.EntityFrameworkCore;
using OnlineLearningPlatformApi.Application.IServices;
using OnlineLearningPlatformApi.Application.Responses;
using OnlineLearningPlatformApi.Application.Responses.Message;
using OnlineLearningPlatformApi.Domain.Entities;


namespace OnlineLearningPlatformApi.Application.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _uow;

        public MessageService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse> SendMessageAsync(Guid senderId, Guid receiverId, string content)
        {
            var response = new ApiResponse();
            try
            {
                var sender = await _uow.Users.GetAsync(u => u.UserId == senderId);
                var receiver = await _uow.Users.GetAsync(u => u.UserId == receiverId);
                if (sender == null || receiver == null) return response.SetBadRequest("User not found");

                bool isAllowed = false;
                if (sender.Role == 0) // Admin
                {
                    // Admin can only message Teachers
                    isAllowed = receiver.Role == 1;
                }
                else if (sender.Role == 1) // Instructor (Teacher)
                {
                    // Teacher can message Admins or their enrolled students
                    if (receiver.Role == 0)
                    {
                        isAllowed = true;
                    }
                    else if (receiver.Role == 2)
                    {
                        isAllowed = await _uow.Enrollments.GetQueryable()
                            .Include(e => e.Course)
                            .AnyAsync(e => e.UserId == receiverId && e.Course.CreatedBy == senderId && !e.IsDeleted && (e.Status == 1 || e.Status == 2));
                    }
                }
                else if (sender.Role == 2) // Student
                {
                    // Student can only message Teachers of courses they registered (No Admins)
                    if (receiver.Role == 1)
                    {
                        isAllowed = await _uow.Enrollments.GetQueryable()
                            .Include(e => e.Course)
                            .AnyAsync(e => e.UserId == senderId && e.Course.CreatedBy == receiverId && !e.IsDeleted && (e.Status == 1 || e.Status == 2));
                    }
                }

                if (!isAllowed)
                {
                    return response.SetBadRequest("You are not authorized to send messages to this user.");
                }

                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                await _uow.BeginTransactionAsync();
                await _uow.Messages.AddAsync(message);
                await _uow.CommitAsync();

                var result = new MessageResponse
                {
                    MessageId = message.MessageId,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    Content = message.Content,
                    SentAt = message.SentAt,
                    IsRead = message.IsRead,
                    SenderName = sender?.FullName ?? "Unknown"
                };

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetConversationAsync(Guid currentUserId, Guid partnerId)
        {
            var response = new ApiResponse();
            try
            {
                var messages = await _uow.Messages.GetQueryable()
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                                (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();

                var userIds = new[] { currentUserId, partnerId };
                var users = await _uow.Users.GetAllAsync(u => userIds.Contains(u.UserId));
                var userDict = users.ToDictionary(u => u.UserId, u => u.FullName ?? "Unknown");

                var result = messages.Select(m => new MessageResponse
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    SenderName = userDict.ContainsKey(m.SenderId) ? userDict[m.SenderId] : "Unknown"
                }).ToList();

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> MarkMessagesAsReadAsync(Guid currentUserId, Guid senderId)
        {
            var response = new ApiResponse();
            try
            {
                var unreadMessages = await _uow.Messages.GetQueryable()
                    .Where(m => m.SenderId == senderId && m.ReceiverId == currentUserId && !m.IsRead)
                    .ToListAsync();

                if (unreadMessages.Any())
                {
                    foreach (var msg in unreadMessages)
                    {
                        msg.IsRead = true;
                        _uow.Messages.Update(msg);
                    }
                    await _uow.SaveChangeAsync();
                }

                return response.SetOk(true);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> GetChatContactsAsync(Guid currentUserId)
        {
            var response = new ApiResponse();
            try
            {
                var currentUser = await _uow.Users.GetAsync(u => u.UserId == currentUserId);
                if (currentUser == null) return response.SetBadRequest("User not found");

                var allContactIds = new List<Guid>();

                if (currentUser.Role == 0) // Admin
                {
                    // Admin can chat with all Teachers (Role == 1)
                    var teachers = await _uow.Users.GetAllAsync(u => u.Role == 1);
                    allContactIds.AddRange(teachers.Select(t => t.UserId));
                }
                else if (currentUser.Role == 1) // Instructor (Teacher)
                {
                    // Teacher can chat with all Admins (Role == 0)
                    var admins = await _uow.Users.GetAllAsync(u => u.Role == 0);
                    allContactIds.AddRange(admins.Select(a => a.UserId));

                    // Teacher can chat with students who enrolled in their courses
                    var enrolledStudents = await _uow.Enrollments.GetQueryable()
                        .Include(e => e.Course)
                        .Where(e => e.Course.CreatedBy == currentUserId && !e.IsDeleted && (e.Status == 1 || e.Status == 2))
                        .Select(e => e.UserId)
                        .Distinct()
                        .ToListAsync();

                    allContactIds.AddRange(enrolledStudents);
                }
                else if (currentUser.Role == 2) // Student
                {
                    // Student can ONLY chat with Teachers of courses they registered (No Admins)
                    var enrolledTeachers = await _uow.Enrollments.GetQueryable()
                        .Include(e => e.Course)
                        .Where(e => e.UserId == currentUserId && !e.IsDeleted && (e.Status == 1 || e.Status == 2))
                        .Select(e => e.Course.CreatedBy)
                        .Distinct()
                        .ToListAsync();

                    allContactIds.AddRange(enrolledTeachers);
                }

                allContactIds = allContactIds.Distinct().ToList();
                allContactIds.Remove(currentUserId);

                var users = await _uow.Users.GetAllAsync(u => allContactIds.Contains(u.UserId));

                // Fetch enrollments to get common courses
                var enrollmentsDict = new Dictionary<Guid, List<string>>();
                if (currentUser.Role == 1) // Teacher -> Student contacts
                {
                    var studentIds = users.Where(u => u.Role == 2).Select(u => u.UserId).ToList();
                    if (studentIds.Any())
                    {
                        var enrolls = await _uow.Enrollments.GetQueryable()
                            .Include(e => e.Course)
                            .Where(e => e.Course.CreatedBy == currentUserId && studentIds.Contains(e.UserId) && !e.IsDeleted && (e.Status == 1 || e.Status == 2))
                            .Select(e => new { e.UserId, e.Course.Title })
                            .ToListAsync();

                        enrollmentsDict = enrolls.GroupBy(e => e.UserId)
                            .ToDictionary(g => g.Key, g => g.Select(x => x.Title).ToList());
                    }
                }
                else if (currentUser.Role == 2) // Student -> Teacher contacts
                {
                    var teacherIds = users.Where(u => u.Role == 1).Select(u => u.UserId).ToList();
                    if (teacherIds.Any())
                    {
                        var enrolls = await _uow.Enrollments.GetQueryable()
                            .Include(e => e.Course)
                            .Where(e => e.UserId == currentUserId && teacherIds.Contains(e.Course.CreatedBy) && !e.IsDeleted && (e.Status == 1 || e.Status == 2))
                            .Select(e => new { TeacherId = e.Course.CreatedBy, e.Course.Title })
                            .ToListAsync();

                        enrollmentsDict = enrolls.GroupBy(e => e.TeacherId)
                            .ToDictionary(g => g.Key, g => g.Select(x => x.Title).ToList());
                    }
                }

                // Fetch last message for each contact to show dynamic preview
                var lastMessages = await _uow.Messages.GetQueryable()
                    .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();
                
                var lastMsgDict = new Dictionary<Guid, Message>();
                foreach (var m in lastMessages)
                {
                    var partnerId = m.SenderId == currentUserId ? m.ReceiverId : m.SenderId;
                    if (!lastMsgDict.ContainsKey(partnerId))
                    {
                        lastMsgDict[partnerId] = m;
                    }
                }

                var result = users.Select(u => {
                    var userCourses = enrollmentsDict.ContainsKey(u.UserId) ? enrollmentsDict[u.UserId] : new List<string>();
                    string roleLabel = u.Role == 0 ? "Admin" : (u.Role == 1 ? "Giảng viên" : "Học sinh");
                    var hasLastMsg = lastMsgDict.ContainsKey(u.UserId);
                    var lastMsgContent = hasLastMsg ? lastMsgDict[u.UserId].Content : "Chưa có tin nhắn nào...";
                    var lastTime = hasLastMsg ? lastMsgDict[u.UserId].SentAt.ToString("o") : ""; // use ISO format

                    return new
                    {
                        id = u.UserId,
                        name = u.FullName ?? u.Email,
                        lastMessage = lastMsgContent,
                        lastTime = lastTime,
                        unread = 0,
                        isOnline = false,
                        role = u.Role,
                        roleLabel = roleLabel,
                        courses = userCourses
                    };
                }).ToList();

                return response.SetOk(result);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest(ex.Message);
            }
        }
    }
}