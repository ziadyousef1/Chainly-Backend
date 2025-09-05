using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Domain.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal LocationLongitude { get; set; }
        public decimal LocationLatitude { get; set; }
        public string Sector { get; set; }
        public string FacilityType { get; set; }
        public int? CompanyId { get; set; }
        public Company Company { get; set; }

        public ICollection<SupplierMaterial> SupplierMaterials { get; set; }
    }
}