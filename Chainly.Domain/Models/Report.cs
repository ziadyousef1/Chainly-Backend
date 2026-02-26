using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Domain.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int ProductionLineId { get; set; }
        public ProductionLine ProductionLine { get; set; }
        public int GoodProducts { get; set; }
        public int DefectiveProducts { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? VideoName { get; set; }
        [NotMapped] public int TotalProducts => GoodProducts + DefectiveProducts;

        [NotMapped]
        public double DefectiveRatio =>
            TotalProducts == 0 ? 0 : Math.Round((double)DefectiveProducts / TotalProducts * 100, 2);

        [NotMapped]
        public double GoodRatio => TotalProducts == 0 ? 0 : Math.Round((double)GoodProducts / TotalProducts * 100, 2);
    }
}