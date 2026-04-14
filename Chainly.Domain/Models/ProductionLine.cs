using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Domain.Models
{
    public class ProductionLine
    {
        public int Id { get; set; }
        public string LineName { get; set; }
        public int CompanyId { get; set; }
        public Company Company { get; set; }
        public string? Description { get; set; }
        public double MaximumSpeed { get; set; } // units/hour
        public double RatedPower { get; set; } // Watts (W)
        public double MaximumTemperature { get; set; } // Celsius (°C)
        public double MaximumCurrent { get; set; } // Amperes (A)
        public ICollection<Report> Reports { get; set; }
        public Camera Camera { get; set; }
    }
}