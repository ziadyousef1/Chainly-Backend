using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Domain.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public decimal LocationLongitude { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public decimal LocationLatitude { get; set; }

        public string? Logo { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}