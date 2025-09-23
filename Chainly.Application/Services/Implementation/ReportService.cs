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
            DefectiveType = "Unknown"
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
            query = query.Where(r => r.ReportDate.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(r => r.ReportDate.Date <= endDate.Value.Date);

        var totalCount = await query.CountAsync();

        var reports = await query
            .OrderByDescending(r => r.ReportDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.VideoName,
                r.ReportDate
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
            ReportDate = report.ReportDate,
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
}