using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Infrastructure.MailService
{
    public class VerifyCodeRequest
    {
        public string Email { get; set; }

        public int Code { get; set; }
    }
}
