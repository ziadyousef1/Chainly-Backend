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
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public OrderStatus Status { get; set; }
        public string Notes { get; set; }
    }
}
