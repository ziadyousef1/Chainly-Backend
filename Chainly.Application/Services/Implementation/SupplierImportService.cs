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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                Console.WriteLine("Removing existing supplier-material relationships...");
                _context.SupplierMaterials.RemoveRange(_context.SupplierMaterials);
                await _context.SaveChangesAsync();

                
                Console.WriteLine("Removing existing suppliers...");
                var oldSuppliers = _context.Suppliers
                    .Where(s => !companyId.HasValue || s.CompanyId == companyId);
                _context.Suppliers.RemoveRange(oldSuppliers);
                await _context.SaveChangesAsync();

                
                Console.WriteLine("Adding new suppliers...");
                var newSuppliers = suppliers
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .Select(dto => MapToSupplier(dto, companyId))
                    .ToList();

                
                if (newSuppliers.Any(s => s.Id > 0))
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Suppliers ON");
                }

                _context.Suppliers.AddRange(newSuppliers);
                var importedCount = await _context.SaveChangesAsync();

                if (newSuppliers.Any(s => s.Id > 0))
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Suppliers OFF");
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

            var worksheet = package.Workbook.Worksheets["Materials"];
            if (worksheet == null)
            {
                Console.WriteLine("Sheet 'Materials' not found in Excel file.");
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

            Console.WriteLine($"Read {materials.Count} materials from Excel.");

            return Task.FromResult<IEnumerable<MaterialImportDto>>(materials);
        }


        public async Task<int> ImportMaterialsAsync(IEnumerable<MaterialImportDto> materials)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                
                Console.WriteLine("Removing existing supplier-material relationships...");
                _context.SupplierMaterials.RemoveRange(_context.SupplierMaterials);
                await _context.SaveChangesAsync();

                
                Console.WriteLine("Removing existing materials...");
                _context.Materials.RemoveRange(_context.Materials);
                await _context.SaveChangesAsync();

                
                Console.WriteLine("Adding new materials...");
                var newMaterials = materials
                    .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                    .Select(MapToMaterial)
                    .ToList();

                
                if (newMaterials.Any(m => m.Id > 0))
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Materials ON");
                }

                _context.Materials.AddRange(newMaterials);
                var importedCount = await _context.SaveChangesAsync();

                if (newMaterials.Any(m => m.Id > 0))
                {
                    await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Materials OFF");
                }

                await transaction.CommitAsync();
                Console.WriteLine($"Imported {importedCount} materials with preserved IDs");
                return importedCount;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error importing materials: {ex.Message}");
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
                    Console.WriteLine(" Invalid: Empty Name");
                    isValid = false;
                }


                if (material.EmissionValue < 0)
                {
                    Console.WriteLine($"Note: Negative EmissionValue for '{material.Name}' (stored as-is)");
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

            // Debug: List all available worksheets
            Console.WriteLine("Available worksheets in Excel file:");
            for (int i = 0; i < package.Workbook.Worksheets.Count; i++)
            {
                Console.WriteLine($"Sheet {i}: '{package.Workbook.Worksheets[i].Name}'");
            }

           
            var worksheet = package.Workbook.Worksheets["SupplierMaterial"] ??
                           package.Workbook.Worksheets["suppliermaterial"] ??
                           package.Workbook.Worksheets["SupplierMaterials"] ??
                           package.Workbook.Worksheets["Sheet3"] ??
                           (package.Workbook.Worksheets.Count >= 3 ? package.Workbook.Worksheets[2] : null);

            if (worksheet == null)
            {
                Console.WriteLine("SupplierMaterial sheet not found. Available sheets:");
                foreach (var ws in package.Workbook.Worksheets)
                {
                    Console.WriteLine($"- {ws.Name}");
                }
                return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
            }

            Console.WriteLine($"Using worksheet: '{worksheet.Name}'");
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            Console.WriteLine($"Total rows in sheet: {rowCount}");

            if (rowCount <= 1)
            {
                Console.WriteLine("No data rows found (only header or empty sheet)");
                return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
            }

            
            Console.WriteLine("Header row content:");
            for (int col = 1; col <= (worksheet.Dimension?.Columns ?? 0); col++)
            {
                var headerValue = worksheet.Cells[1, col].Value?.ToString();
                Console.WriteLine($"Column {col}: '{headerValue}'");
            }

            Console.WriteLine("Reading SupplierMaterials from Excel:");
            Console.WriteLine("Row | SupplierId | MaterialId | SupplierProduct | MatchedMaterial");

            for (int row = 2; row <= rowCount; row++)
            {
                // Debug: Show raw cell values for first few rows
                if (row <= 5)
                {
                    Console.WriteLine($"Raw values for row {row}:");
                    for (int col = 1; col <= 6; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString();
                        Console.WriteLine($"  Col {col}: '{cellValue}'");
                    }
                }

                // Based on your Excel structure:
                // Column 1: SupplierId, Column 4: MaterialId
                var supplierIdValue = GetIntCellValue(worksheet, row, 1); 
                var materialIdValue = GetIntCellValue(worksheet, row, 4);  
                var supplierProduct = GetStringCellValue(worksheet, row, 2); 
                var matchedMaterial = GetStringCellValue(worksheet, row, 5); 

                Console.WriteLine($"{row} | {supplierIdValue} | {materialIdValue} | {supplierProduct} | {matchedMaterial}");

                if (supplierIdValue.HasValue && materialIdValue.HasValue)
                {
                    supplierMaterials.Add(new SupplierMaterialImportDto
                    {
                        SupplierId = supplierIdValue.Value,
                        MaterialId = materialIdValue.Value
                    });
                    Console.WriteLine($"Added relationship: Supplier {supplierIdValue} -> Material {materialIdValue}");
                }
                else
                {
                    Console.WriteLine($"Skipping row {row}: SupplierId={supplierIdValue}, MaterialId={materialIdValue}");
                    if (!supplierIdValue.HasValue)
                        Console.WriteLine($"  - Could not parse SupplierId from column A");
                    if (!materialIdValue.HasValue)
                        Console.WriteLine($"  - Could not parse MaterialId from column D");
                }
            }

            Console.WriteLine($"Total supplier-material relationships read: {supplierMaterials.Count}");
            return Task.FromResult<IEnumerable<SupplierMaterialImportDto>>(supplierMaterials);
        }

        public async Task<int> ImportSupplierMaterialsAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
        {
            if (supplierMaterials == null || !supplierMaterials.Any())
            {
                Console.WriteLine("No supplier-material relationships to import");
                return 0;
            }

            Console.WriteLine($"Starting import of {supplierMaterials.Count()} supplier-material relationships...");

            using var transaction = await _context.Database.BeginTransactionAsync();

            _context.SupplierMaterials.RemoveRange(_context.SupplierMaterials);
            await _context.SaveChangesAsync();

            int importedCount = 0;
            int skippedCount = 0;

            
            var existingSuppliers = await _context.Suppliers.Select(s => s.Id).ToListAsync();
            var existingMaterials = await _context.Materials.Select(m => m.Id).ToListAsync();

            Console.WriteLine($"Database has {existingSuppliers.Count} suppliers and {existingMaterials.Count} materials");
            Console.WriteLine($"Supplier IDs in database: [{string.Join(", ", existingSuppliers.Take(10))}]{(existingSuppliers.Count > 10 ? "..." : "")}");
            Console.WriteLine($"Material IDs in database: [{string.Join(", ", existingMaterials.Take(10))}]{(existingMaterials.Count > 10 ? "..." : "")}");

            foreach (var smDto in supplierMaterials.DistinctBy(x => new { x.SupplierId, x.MaterialId }))
            {
                bool supplierExists = existingSuppliers.Contains(smDto.SupplierId);
                bool materialExists = existingMaterials.Contains(smDto.MaterialId);

                Console.WriteLine($"Checking relationship: Supplier {smDto.SupplierId} -> Material {smDto.MaterialId}");
                Console.WriteLine($"  Supplier exists: {supplierExists}, Material exists: {materialExists}");

                if (!supplierExists || !materialExists)
                {
                    Console.WriteLine($"  SKIPPED: Missing entities in database");
                    skippedCount++;
                    continue;
                }

                _context.SupplierMaterials.Add(new SupplierMaterial
                {
                    SupplierId = smDto.SupplierId,
                    MaterialId = smDto.MaterialId
                });
                importedCount++;
                Console.WriteLine($"  ADDED: Relationship created");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"Import completed: {importedCount} relationships imported, {skippedCount} skipped");
            return importedCount;
        }

        public Task<bool> ValidateSupplierMaterialDataAsync(IEnumerable<SupplierMaterialImportDto> supplierMaterials)
        {
            if (supplierMaterials == null || !supplierMaterials.Any())
                return Task.FromResult(false);


            var validCount = supplierMaterials.Count(sm => sm.SupplierId > 0 && sm.MaterialId > 0);
            return Task.FromResult(validCount > 0);
        }

        /// <summary>
        /// Imports all data from Excel file: Suppliers, Materials, and their relationships
        /// </summary>
        public async Task<(int SuppliersImported, int MaterialsImported, int RelationshipsImported)> ImportAllDataFromExcelAsync(string filePath, int? companyId = null)
        {
            Console.WriteLine("Starting complete Excel import process...");

           
            Console.WriteLine("Step 1: Reading and importing suppliers...");
            var suppliers = await ReadSuppliersFromExcelAsync(filePath);
            var suppliersImported = await ImportSuppliersAsync(suppliers, companyId);
            Console.WriteLine($"Imported {suppliersImported} suppliers");

            
            Console.WriteLine("Step 2: Reading and importing materials...");
            var materials = await ReadMaterialsFromExcelAsync(filePath);
            var materialsImported = await ImportMaterialsAsync(materials);
            Console.WriteLine($"Imported {materialsImported} materials");

            
            Console.WriteLine("Step 3: Reading and importing supplier-material relationships...");
            var supplierMaterials = await ReadSupplierMaterialsFromExcelAsync(filePath);
            var relationshipsImported = await ImportSupplierMaterialsAsync(supplierMaterials);
            Console.WriteLine($"Imported {relationshipsImported} supplier-material relationships");

            Console.WriteLine("Complete Excel import process finished!");
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
                Console.WriteLine($"Extracted number {firstNumberResult} from text '{cellText}' at row {row}, col {col}");
                return firstNumberResult;
            }

            
            var match = System.Text.RegularExpressions.Regex.Match(cellText, @"^\d+");
            if (match.Success && int.TryParse(match.Value, out var regexResult))
            {
                Console.WriteLine($"Regex extracted number {regexResult} from text '{cellText}' at row {row}, col {col}");
                return regexResult;
            }

            Console.WriteLine($"Could not extract integer from '{cellText}' at row {row}, col {col}");
            return null;
        }

        private decimal? GetDecimalCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            var cell = worksheet.Cells[row, col];
            Console.WriteLine($"Cell : {cell.Value}");

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

                    Console.WriteLine($"Cell : {cell.Value}");

                    string? raw = cell.Value.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(raw))
                        return null;

                    
                    if (IsTextualAddress(raw))
                    {
                        Console.WriteLine($"Skipping textual address/name at row {row}, col {col}: '{raw}'");
                        return null;
                    }

                    
                    raw = raw
                        .Replace("−", "-")
                        .Replace("–", "-")
                        .Replace(",", ".")
                        .Replace("٫", ".")
                        .Replace("?", ".")
                        .Replace("٬", ".");

                    
                    if (decimal.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
                    {
                        Console.WriteLine($"Parsed decimal: {result}");
                        return result;
                    }

                    
                    if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleResult))
                    {
                        Console.WriteLine($"Parsed as double then decimal: {(decimal)doubleResult}");
                        return Convert.ToDecimal(doubleResult);
                    }

                    Console.WriteLine($"Failed to parse decimal value: '{raw}' at row {row}, col {col}");
                    return null;
            }
        }

        /// <summary>
        /// Checks if a string looks like a textual address or name rather than coordinates
        /// </summary>
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
}

