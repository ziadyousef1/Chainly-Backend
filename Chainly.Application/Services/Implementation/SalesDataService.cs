using Chainly.Application.DTOs;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Chainly.Application.Services.Implementation
{
    public class SalesDataService : ISalesDataService
    {
        private readonly AppDbContext _context;

        public SalesDataService(AppDbContext context)
        {
            _context = context;
            ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");
        }

        public async Task<SalesDataResponseDto?> GetSalesDataByIdAsync(long id)
        {
            var salesData = await _context.SalesData
                .Where(s => s.Id == id)
                .Select(s => new SalesDataResponseDto
                {
                    CityId = s.CityId,
                    StoreId = s.StoreId,
                    ManagementGroupId = s.ManagementGroupId,
                    FirstCategoryId = s.FirstCategoryId,
                    SecondCategoryId = s.SecondCategoryId,
                    ThirdCategoryId = s.ThirdCategoryId,
                    ProductId = s.ProductId,
                    Date = s.Dt,
                    SaleAmount = s.SaleAmount,
                    HoursSale = s.HoursSale,
                    StockHour6To22Count = s.StockHour6_22Cnt,
                    HoursStockStatus = s.HoursStockStatus,
                    Discount = s.Discount,
                    HolidayFlag = s.HolidayFlag,
                    ActivityFlag = s.ActivityFlag,
                    Precipitation = s.Precpt,
                    AverageTemperature = s.AvgTemperature,
                    AverageHumidity = s.AvgHumidity,
                    AverageWindLevel = s.AvgWindLevel
                })
                .FirstOrDefaultAsync();

            return salesData;
        }

        public async Task<List<SalesDataResponseDto>> GetFilteredSalesDataAsync(SalesDataFilterDto filter)
        {
            var query = _context.SalesData.AsQueryable();

            if (filter.CityId.HasValue)
                query = query.Where(s => s.CityId == filter.CityId.Value);
            
            if (filter.StoreId.HasValue)
                query = query.Where(s => s.StoreId == filter.StoreId.Value);
                
            if (filter.ManagementGroupId.HasValue)
                query = query.Where(s => s.ManagementGroupId == filter.ManagementGroupId.Value);
                
            if (filter.FirstCategoryId.HasValue)
                query = query.Where(s => s.FirstCategoryId == filter.FirstCategoryId.Value);
                
            if (filter.SecondCategoryId.HasValue)
                query = query.Where(s => s.SecondCategoryId == filter.SecondCategoryId.Value);
                
            if (filter.ThirdCategoryId.HasValue)
                query = query.Where(s => s.ThirdCategoryId == filter.ThirdCategoryId.Value);
                
            if (filter.ProductId.HasValue)
                query = query.Where(s => s.ProductId == filter.ProductId.Value);

            var salesData = await query
                .OrderByDescending(s => s.Dt) 
                .Take(filter.Limit ?? 30)
                .Select(s => new SalesDataResponseDto
                {
                    CityId = s.CityId,
                    StoreId = s.StoreId,
                    ManagementGroupId = s.ManagementGroupId,
                    FirstCategoryId = s.FirstCategoryId,
                    SecondCategoryId = s.SecondCategoryId,
                    ThirdCategoryId = s.ThirdCategoryId,
                    ProductId = s.ProductId,
                    Date = s.Dt,
                    SaleAmount = s.SaleAmount,
                    HoursSale = s.HoursSale,
                    StockHour6To22Count = s.StockHour6_22Cnt,
                    HoursStockStatus = s.HoursStockStatus,
                    Discount = s.Discount,
                    HolidayFlag = s.HolidayFlag,
                    ActivityFlag = s.ActivityFlag,
                    Precipitation = s.Precpt,
                    AverageTemperature = s.AvgTemperature,
                    AverageHumidity = s.AvgHumidity,
                    AverageWindLevel = s.AvgWindLevel
                })
                .ToListAsync();

            return salesData;
        }

        public async Task<IEnumerable<SalesDataImportDto>> ReadSalesDataFromExcelAsync(Stream excelStream)
        {
            var salesDataList = new List<SalesDataImportDto>();
            ExcelPackage.License.SetNonCommercialPersonal("Ziad Yousef");
            
            using var package = new ExcelPackage(excelStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null) return salesDataList;

            var rowCount = 15000;
            for (int row = 2; row <= rowCount; row++) 
            {
                var salesData = new SalesDataImportDto
                {
                    CityId = long.Parse(worksheet.Cells[row, 1].Text),
                    StoreId = long.Parse(worksheet.Cells[row, 2].Text),
                    ManagementGroupId = long.Parse(worksheet.Cells[row, 3].Text),
                    FirstCategoryId = long.Parse(worksheet.Cells[row, 4].Text),
                    SecondCategoryId = long.Parse(worksheet.Cells[row, 5].Text),
                    ThirdCategoryId = long.Parse(worksheet.Cells[row, 6].Text),
                    ProductId = long.Parse(worksheet.Cells[row, 7].Text),
                    Dt = worksheet.Cells[row, 8].Text,
                    SaleAmount = double.Parse(worksheet.Cells[row, 9].Text),
                    HoursSale = worksheet.Cells[row, 10].Text,
                    StockHour6_22Cnt = int.Parse(worksheet.Cells[row, 11].Text),
                    HoursStockStatus = worksheet.Cells[row, 12].Text,
                    Discount = double.Parse(worksheet.Cells[row, 13].Text),
                    HolidayFlag = int.Parse(worksheet.Cells[row, 14].Text),
                    ActivityFlag = int.Parse(worksheet.Cells[row, 15].Text),
                    Precpt = double.Parse(worksheet.Cells[row, 16].Text),
                    AvgTemperature = double.Parse(worksheet.Cells[row, 17].Text),
                    AvgHumidity = double.Parse(worksheet.Cells[row, 18].Text),
                    AvgWindLevel = double.Parse(worksheet.Cells[row, 19].Text)
                };

                salesDataList.Add(salesData);
            }

            return salesDataList;
        }



        public async Task<int> ImportSalesDataAsync(IEnumerable<SalesDataImportDto> salesData)
        {
            var salesDataEntities = salesData.Select(data => new SalesData
            {
                CityId = data.CityId,
                StoreId = data.StoreId,
                ManagementGroupId = data.ManagementGroupId,
                FirstCategoryId = data.FirstCategoryId,
                SecondCategoryId = data.SecondCategoryId,
                ThirdCategoryId = data.ThirdCategoryId,
                ProductId = data.ProductId,
                Dt = data.Dt,
                SaleAmount = data.SaleAmount,
                HoursSale = data.HoursSale ?? string.Empty,
                StockHour6_22Cnt = data.StockHour6_22Cnt,
                HoursStockStatus = data.HoursStockStatus ?? string.Empty,
                Discount = data.Discount,
                HolidayFlag = data.HolidayFlag,
                ActivityFlag = data.ActivityFlag,
                Precpt = data.Precpt,
                AvgTemperature = data.AvgTemperature,
                AvgHumidity = data.AvgHumidity,
                AvgWindLevel = data.AvgWindLevel,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SalesData.AddRangeAsync(salesDataEntities);
            return await _context.SaveChangesAsync();
        }
    }
}