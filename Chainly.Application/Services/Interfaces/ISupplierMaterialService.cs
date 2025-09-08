using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.Services.Interfaces
{
    public interface ISupplierMaterialService
    {
        Task<bool> UpdateQuantityAsync(int supplierId, int materialId, decimal quantity);
        Task<bool> UpdateQuantitiesBulkAsync(int supplierId, Dictionary<int, decimal> materialQuantities);
    }
}
