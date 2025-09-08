using Chainly.Application.DTOs.Suppliers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chainly.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MaterialController(ISupplierService supplierService, ISupplierImportService supplierImportService) : BaseController
    {
        [HttpPut("{supplierId}/materials/{materialId}")]
        public async Task<IActionResult> UpdateSupplierMaterialQuantity(
    int supplierId,
    int materialId,
    [FromBody] decimal quantity,
    [FromServices] ISupplierMaterialService supplierMaterialService)
        {
            var updated = await supplierMaterialService.UpdateQuantityAsync(supplierId, materialId, quantity);

            if (!updated)
                return NotFound($"Material {materialId} not found for Supplier {supplierId}");

            return Ok(new { Message = "Quantity updated successfully", SupplierId = supplierId, MaterialId = materialId, NewQuantity = quantity });
        }

        [HttpPut("{supplierId}/materials")]
        public async Task<IActionResult> UpdateSupplierMaterialsQuantities(
        int supplierId,
        [FromBody] List<MaterialQuantityUpdateDto> updates,
        [FromServices] ISupplierMaterialService supplierMaterialService)
        {
            var materialQuantities = updates.ToDictionary(u => u.MaterialId, u => u.Quantity);

            var updated = await supplierMaterialService.UpdateQuantitiesBulkAsync(supplierId, materialQuantities);

            if (!updated)
                return NotFound($"No matching materials found for Supplier {supplierId}");

            return Ok(new { Message = "Quantities updated successfully", SupplierId = supplierId, Updates = updates });
        }
    }
}
