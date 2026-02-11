using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Domain.Models
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public string? VerificationCode { get; set; }
        public DateTime? ExpirationCode { get; set; }
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }
        public string? ProfilePicture { get; set; }

        public List<RefreshToken>? RefreshTokens { get; set; }
    }
}