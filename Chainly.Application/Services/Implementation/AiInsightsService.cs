using Chainly.Application.Configurations;
using Chainly.Application.DTOs;
using Chainly.Application.DTOs.AiInsights;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation;

public class AiInsightsService(
    AppDbContext context,
    IExternalApiService externalApiService,
    IOptions<AiApiSettings> aiApiSettings)
    : IAiInsightsService
{
    private readonly AiApiSettings _aiApiSettings = aiApiSettings.Value;

    public async Task<IActionResult> StartSessionAsync(StartSessionRequestDto dto)
    {
        var productionLine = await context.ProductionLines
            .Include(p => p.Camera)
            .FirstOrDefaultAsync(p =>
                p.Id == dto.ProductionLineId &&
                p.CompanyId == dto.CompanyId);

        if (productionLine == null)
            return new NotFoundObjectResult("Production line not found.");

        if (productionLine.Camera == null)
            throw new Exception("Production line has no camera.");

        var runningReport = await context.Reports
            .FirstOrDefaultAsync(r =>
                r.ProductionLineId == dto.ProductionLineId && r.EndedAt == null && r.VideoName == null);

        if (runningReport != null)
            return new BadRequestObjectResult("A session is already running for this production line.");

        var report = new Report
        {
            ProductionLineId = dto.ProductionLineId,
            StartedAt = DateTime.UtcNow,
            GoodProducts = 0,
            DefectiveProducts = 0
        };

        context.Reports.Add(report);
        await context.SaveChangesAsync();

        var request = new
        {
            report_id = report.Id.ToString(),
            camera_source = productionLine.Camera.CameraSource,
            production_line_id = productionLine.Id.ToString()
        };
        
        
        if (_aiApiSettings.SkipInsightsApiCalls)
        {

            return new OkObjectResult(new OpenReportResponseDto
            {
                Status = "success",
                ReportId = report.Id.ToString(),
                Message = "Session started locally. AI Insights API call is skipped by configuration."
            });
        }

        var url = $"{_aiApiSettings.InsightsBaseUrl}/api/reports/open";
        var response = await externalApiService.PostAsync<OpenReportResponseDto>(url, request);

        if (response.Status != "success")
        {
            context.Reports.Remove(report);
            await context.SaveChangesAsync();
        }

        return new OkObjectResult(response);
    }

    public async Task<IActionResult> CloseSessionAsync(CloseSessionRequestDto dto)
    {
        var report = await context.Reports
            .Include(r => r.ProductionLine)
            .FirstOrDefaultAsync(r =>
                r.Id == dto.ReportId &&
                r.ProductionLine.CompanyId == dto.CompanyId
                && r.EndedAt == null
                && r.VideoName == null);

        if (report == null)
            return new NotFoundObjectResult("Report not found or already closed.");
        
        var request = new
        {
            report_id = report.Id.ToString()
        };

        var url = $"{_aiApiSettings.InsightsBaseUrl}/api/reports/close";
        
        var response = await externalApiService.PostAsync<CloseSessionResponseDto>(url, request);

        if (response.Status == "success" && report.EndedAt == null)
        {
            report.EndedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return new OkObjectResult(response);
    }
}