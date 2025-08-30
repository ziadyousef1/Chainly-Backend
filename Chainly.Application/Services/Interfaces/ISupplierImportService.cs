using Chainly.Application.DTOs;
using Chainly.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.Services.Interfaces
{
    public interface ISupplierImportService
    {
        Task<IEnumerable<SupplierImportDto>> ReadSuppliersFromExcelAsync(string filePath);
        Task<int> ImportSuppliersAsync(IEnumerable<SupplierImportDto> suppliers, int? companyId = null);
        Task<bool> ValidateSupplierDataAsync(IEnumerable<SupplierImportDto> suppliers);
        Supplier MapToSupplier(SupplierImportDto dto, int? companyId = null);
    }
}
