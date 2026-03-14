using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Reports;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class ReportService(AppDbContext context, IHubContext<Chainly.Infrastructure.Hubs.ReportHub> hubContext) : IReportService
{
    public async Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null, int? productionLineId = null, string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .Where(r => r.ProductionLine.CompanyId == companyId && r.VideoName == null);

        if (startDate.HasValue)
            query = query.Where(r => r.EndedAt.HasValue && r.EndedAt.Value.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt.Date <= endDate.Value);

        if (productionLineId.HasValue)
            query = query.Where(r => r.ProductionLineId == productionLineId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ProductionLine.LineName.Contains(search.Trim()));

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportDto()
            {
                ReportId = r.Id,
                StartedAt = r.StartedAt.ToAppFormat(),
                EndedAt = r.EndedAt.HasValue ? r.EndedAt.Value.ToAppFormat() : null,
                ProductionLineId = r.ProductionLineId,
                ProductionLineName = r.ProductionLine.LineName,
                GoodProducts = r.GoodProducts,
                DefectiveProducts = r.DefectiveProducts,
                TotalProducts = r.TotalProducts,
                DefectiveRatio = r.DefectiveRatio,
                GoodRatio = r.GoodRatio,
            })
            .ToListAsync();

        var response = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = reports
        };

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> GetMonthlyReportSummaryAsync(int companyId, int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var reportsQuery = context.Reports
            .AsNoTracking()
            .Include(r => r.ProductionLine)
            .Where(r => r.ProductionLine.CompanyId == companyId &&
                        r.StartedAt >= startDate &&
                        r.StartedAt <= endDate &&
                        r.VideoName == null);

        var groupedReports = await reportsQuery
            .GroupBy(r => new { r.ProductionLineId, r.ProductionLine.LineName })
            .Select(g => new
            {
                ProductionLineId = g.Key.ProductionLineId,
                ProductionLineName = g.Key.LineName,
                TotalProducts = g.Sum(r => r.GoodProducts + r.DefectiveProducts),
                GoodProducts = g.Sum(r => r.GoodProducts),
                DefectiveProducts = g.Sum(r => r.DefectiveProducts),
                GoodRatio = (g.Sum(r => r.GoodProducts) + g.Sum(r => r.DefectiveProducts)) == 0
                    ? 0
                    : Math.Round(g.Sum(r => r.GoodProducts) /
                        (double)(g.Sum(r => r.GoodProducts) + g.Sum(r => r.DefectiveProducts)) * 100, 2),
                DefectiveRatio = (g.Sum(r => r.GoodProducts) + g.Sum(r => r.DefectiveProducts)) == 0
                    ? 0
                    : Math.Round(g.Sum(r => r.DefectiveProducts) /
                        (double)(g.Sum(r => r.GoodProducts) + g.Sum(r => r.DefectiveProducts)) * 100, 2)
            })
            .ToListAsync();

        var totalProducts = groupedReports.Sum(x => x.TotalProducts);
        var goodProducts = groupedReports.Sum(x => x.GoodProducts);
        var defectiveProducts = groupedReports.Sum(x => x.DefectiveProducts);
        var goodRatio = totalProducts == 0 ? 0 : Math.Round(goodProducts / (double)totalProducts * 100, 2);
        var defectiveRatio = totalProducts == 0 ? 0 : Math.Round(defectiveProducts / (double)totalProducts * 100, 2);

        var totalCount = groupedReports.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedData = groupedReports
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new
        {
            TotalProducts = totalProducts,
            GoodProducts = goodProducts,
            DefectiveProducts = defectiveProducts,
            GoodRatio = goodRatio,
            DefectiveRatio = defectiveRatio,
            Month = targetMonth,
            Year = targetYear,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Items = pagedData
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> AddProductsAsync(int reportId, bool defect, int? count = 1)
    {
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            return new NotFoundObjectResult("Report not found.");

        if (defect)
            report.DefectiveProducts += count ?? 1;
        else
            report.GoodProducts += count ?? 1;

        await context.SaveChangesAsync();

        await hubContext.Clients.Group($"ProductionLine_{report.ProductionLineId}")
            .SendAsync("ProductionLineUpdated", new
            {
                ProductionLineId = report.ProductionLineId,
                ReportId = report.Id,
                GoodProducts = report.GoodProducts,
                DefectiveProducts = report.DefectiveProducts,
                TotalProducts = report.GoodProducts + report.DefectiveProducts
            });

        return new OkObjectResult("Products updated successfully.");
    }

    public async Task<IActionResult> GetProductionLineReport(int companyId, int productionLineId, int pageNumber = 1,
        int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .AsNoTracking()
            .Where(r => r.ProductionLineId == productionLineId &&
                        r.ProductionLine.CompanyId == companyId &&
                        r.VideoName == null);

        if (startDate.HasValue)
            query = query.Where(r => r.EndedAt.HasValue && r.EndedAt.Value.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt.Date <= endDate.Value);

        var summary = await query
            .GroupBy(r => new { r.ProductionLineId, r.ProductionLine.LineName })
            .Select(g => new
            {
                g.Key.ProductionLineId,
                ProductionLineName = g.Key.LineName,
                GoodProducts = g.Sum(x => x.GoodProducts),
                DefectiveProducts = g.Sum(x => x.DefectiveProducts),
                TotalProducts = g.Sum(x => x.GoodProducts + x.DefectiveProducts),
                GoodRatio = (g.Sum(x => x.GoodProducts) + g.Sum(x => x.DefectiveProducts)) == 0
                    ? 0
                    : Math.Round(
                        g.Sum(x => x.GoodProducts) /
                        (double)(g.Sum(x => x.GoodProducts) + g.Sum(x => x.DefectiveProducts)) * 100, 2),
                DefectiveRatio = (g.Sum(x => x.GoodProducts) + g.Sum(x => x.DefectiveProducts)) == 0
                    ? 0
                    : Math.Round(
                        g.Sum(x => x.DefectiveProducts) /
                        (double)(g.Sum(x => x.GoodProducts) + g.Sum(x => x.DefectiveProducts)) * 100, 2)
            })
            .FirstOrDefaultAsync();

        if (summary == null)
            return new NotFoundObjectResult(new
                { message = "No data found for this production line in the selected range." });

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                ReportId = r.Id,
                StartedAt = r.StartedAt.ToAppFormat(),
                EndedAt = r.EndedAt.HasValue ? r.EndedAt.Value.ToAppFormat() : null,
                GoodProducts = r.GoodProducts,
                DefectiveProducts = r.DefectiveProducts,
                TotalProducts = r.TotalProducts,
                GoodRatio = r.GoodRatio,
                DefectiveRatio = r.DefectiveRatio
            })
            .ToListAsync();

        var result = new
        {
            Summary = summary,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = reports
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetProductionLineReportsAsync(int companyId, int productionLineId, int pageNumber = 1,
        int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .AsNoTracking()
            .Where(r => r.ProductionLineId == productionLineId &&
                        r.ProductionLine.CompanyId == companyId &&
                        r.VideoName == null);

        if (startDate.HasValue)
            query = query.Where(r => r.EndedAt.HasValue && r.EndedAt.Value.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt.Date <= endDate.Value);

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportDto()
            {
                ReportId = r.Id,
                StartedAt = r.StartedAt.ToAppFormat(),
                EndedAt = r.EndedAt.HasValue ? r.EndedAt.Value.ToAppFormat() : null,
                ProductionLineId = r.ProductionLineId,
                ProductionLineName = r.ProductionLine.LineName,
                GoodProducts = r.GoodProducts,
                DefectiveProducts = r.DefectiveProducts,
                TotalProducts = r.TotalProducts,
                DefectiveRatio = r.DefectiveRatio,
                GoodRatio = r.GoodRatio,
            })
            .ToListAsync();

        var result = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = reports
        };

        return new OkObjectResult(result);
    }


    public async Task<IActionResult> GetReportSummaryByIdAsync(int companyId, int reportId)
    {
        var report = await context.Reports
            .AsNoTracking()
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.ProductionLine.CompanyId == companyId && r.VideoName == null);

        if (report == null)
        {
            return new NotFoundObjectResult(new { message = "Report not found." });
        }

        var totalProducts = report.GoodProducts + report.DefectiveProducts;
        var flawlessPercentage = totalProducts > 0 ? Math.Round(report.GoodProducts / (double)totalProducts * 100, 0) : 0;
        var defectedPercentage = totalProducts > 0 ? Math.Round(report.DefectiveProducts / (double)totalProducts * 100, 0) : 0;

        var result = new
        {
            ReportId = report.Id,
            ProductionLineId = report.ProductionLineId,
            ProductionLineName = report.ProductionLine.LineName,
            TotalProducts = totalProducts,
            FlawlessProducts = report.GoodProducts,
            DefectedProducts = report.DefectiveProducts,
            FlawlessPercentage = (int)flawlessPercentage,
            DefectedPercentage = (int)defectedPercentage,
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> DeleteReportAsync(int companyId, int reportId)
    {
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null || report.ProductionLine.CompanyId != companyId)
            return new NotFoundObjectResult(new { message = "Report not found." });

        context.Reports.Remove(report);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Report deleted successfully." });
    }

    public async Task<IActionResult> StoreVideoReportAsync(int companyId, CreateVideoReportDto dto)
    {
        var productionLineExists = await context.ProductionLines
            .AnyAsync(pl => pl.Id == dto.ProductionLineId && pl.CompanyId == companyId);

        if (!productionLineExists)
            return new NotFoundObjectResult(new
            {
                message = "Production line not found for this company."
            });

        var report = new Report
        {
            ProductionLineId = dto.ProductionLineId,
            StartedAt = DateTime.UtcNow,
            EndedAt = null,
            GoodProducts = dto.GoodProducts,
            DefectiveProducts = dto.DefectiveProducts,
            VideoName = dto.VideoName
        };

        await context.Reports.AddAsync(report);
        await context.SaveChangesAsync();

        return new OkObjectResult(new
        {
            ReportId = report.Id,
            message = "Report created successfully."
        });
    }

    public async Task<IActionResult> GetAllVideoReportsAsync(int companyId, string? search = null,
        DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .AsNoTracking()
            .Where(r =>
                r.ProductionLine.CompanyId == companyId &&
                !string.IsNullOrEmpty(r.VideoName));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.VideoName!.Contains(search));


        if (startDate.HasValue)
            query = query.Where(r => r.StartedAt.Date >= startDate.Value);


        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt.Date <= endDate.Value);


        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                ReportId = r.Id,
                StartedAt = r.StartedAt.ToAppFormat(),
                r.VideoName,
                r.ProductionLineId,
                ProductionLineName = r.ProductionLine.LineName
            })
            .ToListAsync();

        return new OkObjectResult(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        });
    }

    public async Task<IActionResult> GetVideoReportByIdAsync(int companyId, int reportId)
    {
        var report = await context.Reports
            .AsNoTracking()
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r =>
                r.Id == reportId &&
                r.ProductionLine.CompanyId == companyId &&
                !string.IsNullOrEmpty(r.VideoName));

        if (report == null)
            return new NotFoundObjectResult(new { message = "Video report not found." });

        var result = new
        {
            ReportId = report.Id,
            report.ProductionLineId,
            ProductionLineName = report.ProductionLine.LineName,
            StartedAt = report.StartedAt.ToAppFormat(),
            report.GoodProducts,
            report.DefectiveProducts,
            report.TotalProducts,
            report.GoodRatio,
            report.DefectiveRatio
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetVideoReportsInsights(int companyId, int? productionLineId = null,
        DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .AsNoTracking()
            .Where(r =>
                r.ProductionLine.CompanyId == companyId &&
                !string.IsNullOrEmpty(r.VideoName));

        if (productionLineId.HasValue)
            query = query.Where(r => r.ProductionLineId == productionLineId.Value);

        if (startDate.HasValue)
            query = query.Where(r => r.StartedAt.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt.Date <= endDate.Value);

        var summaryData = await query
            .GroupBy(r => 1)
            .Select(g => new
            {
                TotalProducts = g.Sum(x => x.GoodProducts + x.DefectiveProducts),
                GoodProducts = g.Sum(x => x.GoodProducts),
                DefectiveProducts = g.Sum(x => x.DefectiveProducts)
            })
            .FirstOrDefaultAsync();

        var totalProducts = summaryData?.TotalProducts ?? 0;
        var goodProducts = summaryData?.GoodProducts ?? 0;
        var defectiveProducts = summaryData?.DefectiveProducts ?? 0;

        var goodRatio = totalProducts == 0 ? 0 : Math.Round(goodProducts / (double)totalProducts * 100, 2);

        var defectiveRatio = totalProducts == 0 ? 0 : Math.Round(defectiveProducts / (double)totalProducts * 100, 2);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                ReportId = r.Id,
                r.VideoName,
                r.ProductionLineId,
                ProductionLineName = r.ProductionLine.LineName,
                StartedAt = r.StartedAt.ToAppFormat(),
                GoodProducts = r.GoodProducts,
                DefectiveProducts = r.DefectiveProducts,
                TotalProducts = r.GoodProducts + r.DefectiveProducts,
                GoodRatio = (r.GoodProducts + r.DefectiveProducts) == 0
                    ? 0
                    : Math.Round(r.GoodProducts / (double)(r.GoodProducts + r.DefectiveProducts) * 100, 2),
                DefectiveRatio = (r.GoodProducts + r.DefectiveProducts) == 0
                    ? 0
                    : Math.Round(r.DefectiveProducts / (double)(r.GoodProducts + r.DefectiveProducts) * 100, 2)
            })
            .ToListAsync();

        var result = new
        {
            Summary = new
            {
                TotalProducts = totalProducts,
                GoodProducts = goodProducts,
                DefectiveProducts = defectiveProducts,
                GoodRatio = goodRatio,
                DefectiveRatio = defectiveRatio
            },
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items
        };

        return new OkObjectResult(result);
    }
}