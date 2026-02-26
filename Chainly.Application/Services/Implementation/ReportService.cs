using Chainly.Application.DTOs;
using Chainly.Application.DTOs.Reports;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class ReportService(AppDbContext context) : IReportService
{
    public async Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null, int? productionLineId = null, string? search = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .Where(r => r.ProductionLine.CompanyId == companyId);

        if (startDate.HasValue)
            query = query.Where(r => r.EndedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt <= endDate.Value);

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
                        r.StartedAt <= endDate);

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

        return new OkObjectResult("Products updated successfully.");
    }

    public async Task<IActionResult> GetProductionLineReport(
        int companyId,
        int productionLineId,
        int pageNumber = 1,
        int pageSize = 10,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .AsNoTracking()
            .Where(r => r.ProductionLineId == productionLineId &&
                        r.ProductionLine.CompanyId == companyId);

        if (startDate.HasValue)
            query = query.Where(r => r.EndedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.StartedAt <= endDate.Value);

        var summary = await query
            .GroupBy(r => new { r.ProductionLineId, r.ProductionLine.LineName })
            .Select(g => new
            {
                g.Key.ProductionLineId,
                ProductionLineName = g.Key.LineName,
                GoodProducts = g.Sum(x => x.GoodProducts),
                DefectiveProducts = g.Sum(x => x.DefectiveProducts),
                TotalProducts = g.Sum(x => x.GoodProducts + x.DefectiveProducts),
                GoodRatio = Math.Round(
                    g.Sum(x => x.GoodProducts) /
                    (double)(g.Sum(x => x.GoodProducts) + g.Sum(x => x.DefectiveProducts)) * 100, 2),
                DefectiveRatio = Math.Round(
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
}