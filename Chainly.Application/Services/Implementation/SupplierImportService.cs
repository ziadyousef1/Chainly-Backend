using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace Chainly.Application.Services.Implementation
{
    public class SupplierImportService : ISupplierImportService
    {
        private readonly AppDbContext _context;

        public SupplierImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SupplierImportDto>> ReadSuppliersFromExcelAsync(string filePath)
        {
            var suppliers = new List<SupplierImportDto>();
            ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef"); using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount <= 1) return suppliers;

            for (int row = 2; row <= rowCount; row++)
            {
                var supplier = new SupplierImportDto
                {
                    Name = GetCellValue(worksheet, row, 3)?.ToString() ?? string.Empty,
                    LocationLatitude = GetCellValue(worksheet, row, 7),
                    LocationLongitude = GetCellValue(worksheet, row, 8),
                    Sector = GetCellValue(worksheet, row, 9)?.ToString(),
                    FacilityType = GetCellValue(worksheet, row, 14)?.ToString()
                };

                if (!string.IsNullOrWhiteSpace(supplier.Name))
                {
                    suppliers.Add(supplier);
                }
            }
            return suppliers;
        }

        public async Task<int> ImportSuppliersAsync(IEnumerable<SupplierImportDto> suppliers, int? companyId = null)
        {
            var importedCount = 0;
            using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var supplierDto in suppliers)
            {
                var existingSupplier = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.Name.ToLower() == supplierDto.Name.ToLower());

                if (existingSupplier == null)
                {
                    var supplier = MapToSupplier(supplierDto, companyId);
                    _context.Suppliers.Add(supplier);
                    importedCount++;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return importedCount;
        }

        public Task<bool> ValidateSupplierDataAsync(IEnumerable<SupplierImportDto> suppliers)
        {
            var errors = new List<string>();

            foreach (var (supplier, index) in suppliers.Select((s, i) => (s, i + 1)))
            {
                if (string.IsNullOrWhiteSpace(supplier.Name))
                    errors.Add($"Row {index + 1}: Name is required");

                if (supplier.LocationLatitude < -90m || supplier.LocationLatitude > 90m)
                    errors.Add($"Row {index + 1}: Invalid latitude value");

                if (supplier.LocationLongitude < -180m || supplier.LocationLongitude > 180m)
                    errors.Add($"Row {index + 1}: Invalid longitude value");
            }

            return Task.FromResult(!errors.Any());
        }

        public Supplier MapToSupplier(SupplierImportDto dto, int? companyId = null)
        {
            return new Supplier
            {
                Name = dto.Name,
                LocationLatitude = dto.LocationLatitude ?? 0,
                LocationLongitude = dto.LocationLongitude ?? 0,
                Sector = dto.Sector ?? "Unknown",
                FacilityType = dto.FacilityType ?? "Unknown",
                CompanyId = companyId
            };
        }

        private decimal? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var value = worksheet.Cells[row, col].Value;
            if (value == null) return 0m;

            return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;

        }

        private bool IsValidLatitude(string latitude)
        {
            if (double.TryParse(latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            {
                return lat >= -90 && lat <= 90;
            }
            return false;
        }

        private bool IsValidLongitude(string longitude)
        {
            if (double.TryParse(longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return lng >= -180 && lng <= 180;
            }
            return false;
        }

        public async Task<IEnumerable<MaterialImportDto>> ReadMaterialsFromExcelAsync(string filePath)
        {
            var materials = new List<MaterialImportDto>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets["Materials"];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount <= 1) return materials;

            for (int row = 2; row <= rowCount; row++)
            {
                var material = new MaterialImportDto
                {
                    Name = worksheet.Cells[row, 1].Text,
                    EmissionValue = ParseDecimal(worksheet.Cells[row, 2].Text) ?? 0,
                    Unit = worksheet.Cells[row, 3].Text
                };

                if (!string.IsNullOrWhiteSpace(material.Name))
                    materials.Add(material);
            }
            return materials;
        }

        public async Task<int> ImportMaterialsAsync(IEnumerable<MaterialImportDto> materials)
        {
            var importedCount = 0;

            foreach (var materialDto in materials)
            {
                var existingMaterial = await _context.Materials
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == materialDto.Name.ToLower());

                if (existingMaterial == null)
                {
                    var material = MapToMaterial(materialDto);
                    _context.Materials.Add(material);
                    importedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return importedCount;
        }

        public Task<bool> ValidateMaterialDataAsync(IEnumerable<MaterialImportDto> materials)
        {
            foreach (var material in materials)
            {
                if (string.IsNullOrWhiteSpace(material.Name))
                    return Task.FromResult(false);

                if (material.EmissionValue < 0)
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        public Material MapToMaterial(MaterialImportDto dto)
        {
            return new Material
            {
                Name = dto.Name,
                EmissionValue = dto.EmissionValue,
                Unit = dto.Unit
            };
        }
        public async Task<IEnumerable<SupplierMaterialImportDto>> ReadSupplierMaterialsFromExcelAsync(string filePath)
        {
            var supplierMaterials = new List<SupplierMaterialImportDto>();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets["SupplierMaterials"];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount <= 1) return supplierMaterials;

            for (int row = 2; row <= rowCount; row++)
            {
                var sm = new SupplierMaterialImportDto
                {
                    SupplierName = worksheet.Cells[row, 1].Text,
                    MaterialName = worksheet.Cells[row, 2].Text
                };

                if (!string.IsNullOrWhiteSpace(sm.SupplierName) && !string.IsNullOrWhiteSpace(sm.MaterialName))
                    supplierMaterials.Add(sm);
            }
            return supplierMaterials;
        }

        public async Task<int> ImportSupplierMaterialsAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
        {
            var importedCount = 0;

            foreach (var smDto in supplierMaterials)
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Name.ToLower() == smDto.SupplierName.ToLower());
                var material = await _context.Materials.FirstOrDefaultAsync(m => m.Name.ToLower() == smDto.MaterialName.ToLower());

                if (supplier != null && material != null)
                {
                    var exists = await _context.SupplierMaterials
                        .AnyAsync(sm => sm.SupplierId == supplier.Id && sm.MaterialId == material.Id);

                    if (!exists)
                    {
                        _context.SupplierMaterials.Add(new SupplierMaterial
                        {
                            SupplierId = supplier.Id,
                            MaterialId = material.Id
                        });
                        importedCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return importedCount;
        }

        public Task<bool> ValidateSupplierMaterialDataAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
        {
            foreach (var sm in supplierMaterials)
            {
                if (string.IsNullOrWhiteSpace(sm.SupplierName) || string.IsNullOrWhiteSpace(sm.MaterialName))
                    return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }


        private decimal? ParseDecimal(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;
            return null;
        }
    }
}
