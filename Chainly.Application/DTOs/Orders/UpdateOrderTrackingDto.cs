using Chainly.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.DTOs.Orders
{
    public class UpdateOrderTrackingDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public OrderStatus Status { get; set; }
        public string Notes { get; set; }
    }
}
