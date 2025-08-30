using Chainly.Application.DTOs;
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
            ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];
            var rowCount = worksheet.Dimension?.Rows ?? 0;

            if (rowCount <= 1) return suppliers;

            for (int row = 2; row <= rowCount; row++)
            {
                var supplier = new SupplierImportDto
                {
                    Name = GetCellValue(worksheet, row, 3)?.ToString() ?? string.Empty,
                    LocationLatitude = GetCellValue(worksheet, row, 7)?.ToString(),
                    LocationLongitude = GetCellValue(worksheet, row, 8)?.ToString(),
                    Sector = GetCellValue(worksheet, row, 9)?.ToString(),
                    FacilityType = GetCellValue(worksheet, row, 14)?.ToString(),
                    ProductType = GetCellValue(worksheet, row, 16)?.ToString()
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

                if (!string.IsNullOrEmpty(supplier.LocationLatitude) && !IsValidLatitude(supplier.LocationLatitude))
                    errors.Add($"Row {index + 1}: Invalid latitude value");

                if (!string.IsNullOrEmpty(supplier.LocationLongitude) && !IsValidLongitude(supplier.LocationLongitude))
                    errors.Add($"Row {index + 1}: Invalid longitude value");
            }

            return Task.FromResult(!errors.Any());
        }

        public Supplier MapToSupplier(SupplierImportDto dto, int? companyId = null)
        {
            return new Supplier
            {
                Name = dto.Name,
                LocationLatitude = dto.LocationLatitude ?? "0",
                LocationLongitude = dto.LocationLongitude ?? "0",
                ProductType = dto.ProductType ?? "Unknown",
                Sector = dto.Sector ?? "Unknown",
                FacilityType = dto.FacilityType ?? "Unknown",
                CompanyId = companyId
            };
        }

        private object? GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            return worksheet.Cells[row, col].Value;
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
    }
}
