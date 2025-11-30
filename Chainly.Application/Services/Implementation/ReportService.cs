using System.Globalization;
using Chainly.Application.DTOs;
using Chainly.Application.DTOs.ProductionLines;
using Chainly.Application.DTOs.Reports;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Chainly.Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using DefectiveProductDto = Chainly.Application.DTOs.DefectiveProductDto;

namespace Chainly.Application.Services.Implementation;

public class ReportService(AppDbContext context) : IReportService
{
    public async Task<IActionResult> StoreSessionDetailsAsync(SessionDetailsResponseDto sessionDto,
        int productionLineId)
    {
        var exists = await context.Reports
            .FirstOrDefaultAsync(r => r.SessionId == sessionDto.SessionId);

        if (exists != null)
            return new ObjectResult(new { reportId = exists.Id });

        var report = new Report
        {
            SessionId = sessionDto.SessionId,
            ProductionLineId = productionLineId,
            ReportDate = DateTime.UtcNow,
            GoodProducts = sessionDto.NonDefectiveCount,
            VideoName = sessionDto.VideoName,
        };

        await context.Reports.AddAsync(report);
        await context.SaveChangesAsync();


        var defective = sessionDto.DefectiveProducts.Select(d => new DefectiveProduct
        {
            ReportId = report.Id,
            ProductId = d.Id,
            Time = DateTime.Parse(d.DetectedAt),
        });

        context.DefectiveProducts.AddRange(defective);
        await context.SaveChangesAsync();

        return new OkObjectResult(new
        {
            message = "Report stored successfully.",
            reportId = report.Id
        });
    }

    public async Task<IActionResult> GetAllReportsAsync(int companyId, int pageNumber = 1, int pageSize = 10,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Reports
            .Where(r => r.ProductionLine.CompanyId == companyId);

        if (startDate.HasValue)
            query = query.Where(r => r.ReportDate.Date.Day >= startDate.Value.Date.Day);

        if (endDate.HasValue)
            query = query.Where(r => r.ReportDate.Date.Day <= endDate.Value.Date.Day);

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.ReportDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                ID = r.Id,
                videoName = r.VideoName,
                reportDate = r.ReportDate.ToAppFormat(),
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

    public async Task<IActionResult> GetReportByIdAsync(int reportId, int companyId, int pageNumber = 1,
        int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .Include(r => r.DefectiveProducts)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null || report.ProductionLine.CompanyId != companyId)
            return new NotFoundObjectResult(new { message = "No report found for the session." });

        var defectiveProductsQuery = report.DefectiveProducts
            .OrderBy(d => d.Time)
            .AsQueryable();

        var totalCount = defectiveProductsQuery.Count();

        var defectiveProductsPage = defectiveProductsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DefectiveProductReportDto
            {
                Id = d.ProductId.ToString(),
                DetectedAt = d.Time.ToString("o")
            })
            .ToList();

        var response = new ReportDto
        {
            ReportId = report.Id,
            VideoName = report.VideoName,
            TotalProducts = report.TotalProducts,
            DefectiveCount = report.DefectiveProductsCount,
            NonDefectiveCount = report.GoodProducts,
            ReportDate = report.ReportDate.ToAppFormat(),
            ProductionLine = new ProductionLineDto
            {
                Id = report.ProductionLineId,
                LineName = report.ProductionLine.LineName,
                Description = report.ProductionLine.Description
            },
            DefectiveProducts = defectiveProductsPage
        };

        var result = new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Report = response
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> GetReportSummaryAsync(
        int companyId,
        int? productionLineId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var query = context.Reports
            .Include(r => r.ProductionLine)
            .Include(r => r.DefectiveProducts)
            .Where(r => r.ProductionLine.CompanyId == companyId);

        if (productionLineId.HasValue)
            query = query.Where(r => r.ProductionLineId == productionLineId.Value);

        if (startDate.HasValue)
            query = query.Where(r => r.ReportDate.Date.Day >= startDate.Value.Date.Day);

        if (endDate.HasValue)
            query = query.Where(r => r.ReportDate.Date.Day <= endDate.Value.Date.Day);

        var reports = await query.ToListAsync();

        var totalDefectiveProducts = reports.SelectMany(r => r.DefectiveProducts).Count();
        var defectiveProductsPaged = reports
            .SelectMany(r => r.DefectiveProducts)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DefectiveProductReportDto()
            {
                Id = d.ProductId,
                DetectedAt = d.Time.ToAppFormat()
            }).ToList();

        var summary = new ReportSummaryDto
        {
            ProductionLineId = productionLineId ?? 0,
            TotalProducts = reports.Sum(r => r.TotalProducts),
            DefectiveProductsCount = totalDefectiveProducts,
            NonDefectiveProductsCount = reports.Sum(r => r.GoodProducts),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalDefectiveProducts / pageSize),
            DefectiveProducts = defectiveProductsPaged
        };

        return new OkObjectResult(summary);
    }

    public async Task<IActionResult> GetMonthlyReportSummaryAsync(int companyId, int? month = null, int? year = null,
        int pageNumber = 1, int pageSize = 10)
    {
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;

        var reportsQuery = context.Reports
            .Include(r => r.ProductionLine)
            .Include(r => r.DefectiveProducts)
            .Where(r => r.ProductionLine.CompanyId == companyId &&
                        r.ReportDate.Month == targetMonth &&
                        r.ReportDate.Year == targetYear)
            .AsNoTracking();

        var allReports = await reportsQuery.ToListAsync();

        var groupedReports = allReports
            .GroupBy(r => new { r.ProductionLine.Id, r.ProductionLine.LineName })
            .Select(g => new
            {
                ProductionLineId = g.Key.Id,
                ProductionLineName = g.Key.LineName,
                TotalProducts = g.Sum(r => r.GoodProducts + r.DefectiveProducts.Count),
                DefectiveProductsCount = g.Sum(r => r.DefectiveProducts.Count),
                NonDefectiveProductsCount = g.Sum(r => r.GoodProducts)
            })
            .ToList();

        var totalCount = groupedReports.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var pagedData = groupedReports
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalProducts = allReports.Sum(r => r.GoodProducts + r.DefectiveProducts.Count);
        var defectiveProducts = allReports.Sum(r => r.DefectiveProducts.Count);
        var nonDefectiveProducts = allReports.Sum(r => r.GoodProducts);

        var result = new
        {
            TotalProducts = totalProducts,
            DefectiveProducts = defectiveProducts,
            NonDefectiveProducts = nonDefectiveProducts,
            Month = targetMonth,
            Year = targetYear,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            Items = pagedData,
        };

        return new OkObjectResult(result);
    }

    public async Task<IActionResult> IncrementGoodProductsAsync(int companyId, int reportId, int count = 1)
    {
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null || report.ProductionLine.CompanyId != companyId)
            return new NotFoundObjectResult("Report not found.");

        report.GoodProducts += count;
        await context.SaveChangesAsync();

        return new OkObjectResult("Good products incremented successfully.");
    }

    public async Task<IActionResult> AddDefectiveProductsAsync(int companyId, int reportId,
        DefectiveProductDto[] defectiveProducts)
    {
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null || report.ProductionLine.CompanyId != companyId)
            return new NotFoundObjectResult(new { message = "Report not found." });

        var newDefective = defectiveProducts.Select(d => new DefectiveProduct
        {
            ReportId = reportId,
            ProductId = d.Id,
            Time = DateTime.Parse(d.DetectedAt)
        });

        await context.DefectiveProducts.AddRangeAsync(newDefective);
        await context.SaveChangesAsync();

        return new OkObjectResult("Defective products added successfully.");
    }

    public async Task<IActionResult> CreateEmptySessionAsync(int companyId, string sessionId, int productionLineId)
    {
        var exists = await context.Reports
            .FirstOrDefaultAsync(r => r.SessionId == sessionId);

        if (exists != null)
            return new ObjectResult(new { message = "Session already exists.", reportId = exists.Id });

        var productionLine = await context.ProductionLines
            .FirstOrDefaultAsync(p => p.Id == productionLineId);

        if (productionLine == null || productionLine.CompanyId != companyId)
        {
            return new NotFoundObjectResult("Production line not found or does not belong to the company.");
        }

        var report = new Report
        {
            SessionId = sessionId,
            ProductionLineId = productionLineId,
            ReportDate = DateTime.UtcNow,
            GoodProducts = 0
        };

        await context.Reports.AddAsync(report);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Session created successfully.", reportId = report.Id });
    }
}