using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chainly.Application.Services.Implementation
{
    public class SupplierMaterialService : ISupplierMaterialService
    {
        private readonly AppDbContext _context;
        public SupplierMaterialService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<bool> UpdateQuantityAsync(int supplierId, int materialId, decimal quantity)
        {
            var supplierMaterial = await _context.SupplierMaterials
                .FirstOrDefaultAsync(sm => sm.SupplierId == supplierId && sm.MaterialId == materialId);

            if (supplierMaterial == null)
                return false;

            //supplierMaterial.Quantity = quantity;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateQuantitiesBulkAsync(int supplierId, Dictionary<int, decimal> materialQuantities)
        {
            var supplierMaterials = await _context.SupplierMaterials
                .Where(sm => sm.SupplierId == supplierId && materialQuantities.Keys.Contains(sm.MaterialId))
                .ToListAsync();

            if (!supplierMaterials.Any())
                return false;

            //foreach (var sm in supplierMaterials)
            //{
            //    sm.Quantity = materialQuantities[sm.MaterialId];
            //}

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
