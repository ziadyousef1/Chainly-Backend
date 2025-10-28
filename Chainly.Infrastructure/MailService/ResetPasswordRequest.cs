using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Infrastructure.MailService
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; }

        public string currentPassword { get; set; }

        public string newPassword { get; set; }
        public string newPasswordConfirm { get; set; }
    }
}