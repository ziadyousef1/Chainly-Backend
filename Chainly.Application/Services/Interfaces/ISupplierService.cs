using Chainly.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.Services.Interfaces
{
    public interface ISupplierService
    {

        Task<PagedResult<SupplierDto>> GetSuppliersAsync(string userId, int? pageNumber = null, int? pageSize = null);
        Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, int companyId);
        Task<bool> UpdateSupplierAsync(int id, SupplierDto dto, int companyId);
        Task<bool> DeleteSupplierAsync(int id, int companyId);
    }
}
