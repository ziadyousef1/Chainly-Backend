using Chainly.Application.DTOs.Suppliers;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;

namespace Chainly.Application.Services.Implementation;

public class SupplierImportService(AppDbContext context) : ISupplierImportService
{
    public async Task<IEnumerable<SupplierImportDto>> ReadSuppliersFromExcelAsync(string filePath)
    {
        var suppliers = new List<SupplierImportDto>();
        ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");
        using var package = new ExcelPackage(new FileInfo(filePath));


        var worksheet = package.Workbook.Worksheets["Suppliers"] ?? package.Workbook.Worksheets[0];
        var rowCount = worksheet.Dimension?.Rows ?? 0;

        if (rowCount <= 1) return suppliers;

        for (int row = 2; row <= rowCount; row++)
        {
            var supplier = new SupplierImportDto
            {
                ExcelId = GetIntCellValue(worksheet, row, 1),
                Name = GetStringCellValue(worksheet, row, 2) ?? string.Empty,
                LocationLatitude = GetDecimalCellValue(worksheet, row, 4),
                LocationLongitude = GetDecimalCellValue(worksheet, row, 3),
                Sector = GetStringCellValue(worksheet, row, 5),
                FacilityType = GetStringCellValue(worksheet, row, 6)
            };

            if (!string.IsNullOrWhiteSpace(supplier.Name))
                suppliers.Add(supplier);
        }

        return suppliers;
    }

    public async Task<int> ImportSuppliersAsync(IEnumerable<SupplierImportDto> suppliers, int? companyId = null)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            Console.WriteLine("Removing existing supplier-material relationships...");
            context.SupplierMaterials.RemoveRange(context.SupplierMaterials);
            await context.SaveChangesAsync();


            Console.WriteLine("Removing existing suppliers...");
            var oldSuppliers = context.Suppliers
                .Where(s => !companyId.HasValue || s.CompanyId == companyId);
            context.Suppliers.RemoveRange(oldSuppliers);
            await context.SaveChangesAsync();


            Console.WriteLine("Adding new suppliers...");
            var newSuppliers = suppliers
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .Select(dto => MapToSupplier(dto, companyId))
                .ToList();


            if (newSuppliers.Any(s => s.Id > 0))
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Suppliers ON");
            }

            context.Suppliers.AddRange(newSuppliers);
            var importedCount = await context.SaveChangesAsync();

            if (newSuppliers.Any(s => s.Id > 0))
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Suppliers OFF");
            }

            await transaction.CommitAsync();
            Console.WriteLine($"Imported {importedCount} suppliers with preserved IDs");
            return importedCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error importing suppliers: {ex.Message}");
            throw;
        }
    }

    public Task<bool> ValidateSupplierDataAsync(IEnumerable<SupplierImportDto> suppliers)
    {
        foreach (var supplier in suppliers)
        {
            if (string.IsNullOrWhiteSpace(supplier.Name))
                return Task.FromResult(false);

            if (supplier.LocationLatitude.HasValue &&
                (supplier.LocationLatitude < -90m || supplier.LocationLatitude > 90m))
                return Task.FromResult(false);

            if (supplier.LocationLongitude.HasValue &&
                (supplier.LocationLongitude < -180m || supplier.LocationLongitude > 180m))
                return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }


    public Supplier MapToSupplier(SupplierImportDto dto, int? companyId = null)
    {
        var supplier = new Supplier
        {
            Name = dto.Name,
            LocationLatitude = dto.LocationLatitude ?? 0,
            LocationLongitude = dto.LocationLongitude ?? 0,
            Sector = string.IsNullOrWhiteSpace(dto.Sector) ? "Unknown" : dto.Sector,
            FacilityType = string.IsNullOrWhiteSpace(dto.FacilityType) ? "Unknown" : dto.FacilityType,
            CompanyId = companyId
        };


        if (dto.ExcelId.HasValue)
        {
            supplier.Id = dto.ExcelId.Value;
        }

        return supplier;
    }

    public Task<IEnumerable<MaterialImportDto>> ReadMaterialsFromExcelAsync(string filePath)
    {
        var materials = new List<MaterialImportDto>();

        ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");
        using var package = new ExcelPackage(new FileInfo(filePath));

        var worksheet = package.Workbook.Worksheets[2];
        if (worksheet == null)
        {
            return Task.FromResult<IEnumerable<MaterialImportDto>>(materials);
        }

        var rowCount = worksheet.Dimension?.Rows ?? 0;
        if (rowCount <= 1) return Task.FromResult<IEnumerable<MaterialImportDto>>(materials);

        for (int row = 2; row <= rowCount; row++)
        {
            var materialId = GetIntCellValue(worksheet, row, 1);
            var name = GetStringCellValue(worksheet, row, 2);
            var unit = GetStringCellValue(worksheet, row, 3);
            var emission = GetDecimalCellValue(worksheet, row, 4);

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(unit) && emission == null)
                continue;

            var material = new MaterialImportDto
            {
                ExcelId = materialId,
                Name = name ?? string.Empty,
                Unit = unit ?? string.Empty,
                EmissionValue = emission.GetValueOrDefault()
            };

            materials.Add(material);
        }

        return Task.FromResult<IEnumerable<MaterialImportDto>>(materials);
    }


    public async Task<int> ImportMaterialsAsync(IEnumerable<MaterialImportDto> materials)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.SupplierMaterials.RemoveRange(context.SupplierMaterials);
            await context.SaveChangesAsync();

            context.Materials.RemoveRange(context.Materials);
            await context.SaveChangesAsync();

            var newMaterials = materials
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .Select(MapToMaterial)
                .ToList();


            if (newMaterials.Any(m => m.Id > 0))
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Materials ON");
            }

            context.Materials.AddRange(newMaterials);
            var importedCount = await context.SaveChangesAsync();

            if (newMaterials.Any(m => m.Id > 0))
            {
                await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Materials OFF");
            }

            await transaction.CommitAsync();
            return importedCount;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<bool> ValidateMaterialDataAsync(IEnumerable<MaterialImportDto> materials)
    {
        var isValid = true;

        foreach (var material in materials)
        {
            if (string.IsNullOrWhiteSpace(material.Name))
            {
                isValid = false;
            }
        }

        return Task.FromResult(isValid);
    }

    public Material MapToMaterial(MaterialImportDto dto)
    {
        var material = new Material
        {
            Name = dto.Name,
            EmissionValue = dto.EmissionValue,
            Unit = dto.Unit
        };


        if (dto.ExcelId.HasValue)
        {
            material.Id = dto.ExcelId.Value;
        }

        return material;
    }

    public Task<IEnumerable<SupplierMaterialImportDto>> ReadSupplierMaterialsFromExcelAsync(string filePath)
    {
        var supplierMaterials = new List<SupplierMaterialImportDto>();
        ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");

        using var package = new ExcelPackage(new FileInfo(filePath));

        var worksheet = package.Workbook.Worksheets[3] ?? null;

        if (worksheet == null)
        {
            return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
        }

        var rowCount = worksheet.Dimension?.Rows ?? 0;

        if (rowCount <= 1)
        {
            return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
        }

        for (int row = 2; row <= rowCount; row++)
        {
            var supplierIdValue = GetIntCellValue(worksheet, row, 1);
            var materialIdValue = GetIntCellValue(worksheet, row, 2);

            if (supplierIdValue.HasValue && materialIdValue.HasValue)
            {
                supplierMaterials.Add(new SupplierMaterialImportDto
                {
                    SupplierId = supplierIdValue.Value,
                    MaterialId = materialIdValue.Value
                });
            }
        }

        return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
    }

    public async Task<int> ImportSupplierMaterialsAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
    {
        if (supplierMaterials == null || !supplierMaterials.Any())
        {
            return 0;
        }

        using var transaction = await context.Database.BeginTransactionAsync();

        context.SupplierMaterials.RemoveRange(context.SupplierMaterials);
        await context.SaveChangesAsync();

        int importedCount = 0;
        int skippedCount = 0;


        var existingSuppliers = await context.Suppliers.Select(s => s.Id).ToListAsync();
        var existingMaterials = await context.Materials.Select(m => m.Id).ToListAsync();

        foreach (var smDto in supplierMaterials.DistinctBy(x => new { x.SupplierId, x.MaterialId }))
        {
            bool supplierExists = existingSuppliers.Contains(smDto.SupplierId);
            bool materialExists = existingMaterials.Contains(smDto.MaterialId);

            if (!supplierExists || !materialExists)
            {
                skippedCount++;
                continue;
            }

            context.SupplierMaterials.Add(new SupplierMaterial
            {
                SupplierId = smDto.SupplierId,
                MaterialId = smDto.MaterialId
            });
            importedCount++;
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return importedCount;
    }

    public Task<bool> ValidateSupplierMaterialDataAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
    {
        if (supplierMaterials == null || !supplierMaterials.Any())
            return Task.FromResult(false);


        var validCount = supplierMaterials.Count(sm => sm.SupplierId > 0 && sm.MaterialId > 0);
        return Task.FromResult(validCount > 0);
    }

    public async Task<(int SuppliersImported, int MaterialsImported, int RelationshipsImported)>
        ImportAllDataFromExcelAsync(string filePath, int? companyId = null)
    {
        var suppliers = await ReadSuppliersFromExcelAsync(filePath);
        var suppliersImported = await ImportSuppliersAsync(suppliers, companyId);

        var materials = await ReadMaterialsFromExcelAsync(filePath);
        var materialsImported = await ImportMaterialsAsync(materials);

        var supplierMaterials = await ReadSupplierMaterialsFromExcelAsync(filePath);
        var relationshipsImported = await ImportSupplierMaterialsAsync(supplierMaterials);

        return (suppliersImported, materialsImported, relationshipsImported);
    }

    private string? GetStringCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        return worksheet.Cells[row, col].Text?.Trim();
    }

    private int? GetIntCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cells[row, col];
        if (cell?.Value == null)
            return null;

        var cellText = cell.Value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(cellText))
            return null;


        if (int.TryParse(cellText, out var directResult))
            return directResult;


        var parts = cellText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && int.TryParse(parts[0], out var firstNumberResult))
        {
            return firstNumberResult;
        }


        var match = System.Text.RegularExpressions.Regex.Match(cellText, @"^\d+");
        if (match.Success && int.TryParse(match.Value, out var regexResult))
        {
            return regexResult;
        }

        return null;
    }

    private decimal? GetDecimalCellValue(ExcelWorksheet worksheet, int row, int col)
    {
        var cell = worksheet.Cells[row, col];

        if (cell?.Value == null)
            return null;


        switch (cell.Value)
        {
            case double d:
                try
                {
                    return Convert.ToDecimal(d);
                }
                catch
                {
                    return (decimal)d;
                }
            case decimal dec:
                return dec;
            case float f:
                return Convert.ToDecimal(f);
            case int i:
                return i;
            case long l:
                return l;
            default:

                string? raw = cell.Value.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    return null;


                if (IsTextualAddress(raw))
                {
                    return null;
                }


                raw = raw
                    .Replace("−", "-")
                    .Replace("–", "-")
                    .Replace(",", ".")
                    .Replace("٫", ".")
                    .Replace("?", ".")
                    .Replace("٬", ".");


                if (decimal.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }


                if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture, out var doubleResult))
                {
                    return Convert.ToDecimal(doubleResult);
                }

                return null;
        }
    }

    private bool IsTextualAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;


        var addressIndicators = new[]
        {
            "city", "street", "block", "cairo", "egypt", "governorate",
            "industrial", "zone", "port", "co", "company", "ltd",
            "group", "textiles", "garments", "denim", "international",
            "spinning", "weaving", "apparel", "trading"
        };

        var lowerValue = value.ToLower();


        var indicatorCount = addressIndicators.Count(indicator => lowerValue.Contains(indicator));

        return indicatorCount >= 2 ||
               lowerValue.Contains("street") ||
               lowerValue.Contains("block") ||
               (lowerValue.Contains("egypt") && lowerValue.Length > 10);
    }
}