using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineLearningPlatformApi.Application.Requests.Admin
{
    public class AdminUpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? Title { get; set; }
        public int Role { get; set; }
    }
}
