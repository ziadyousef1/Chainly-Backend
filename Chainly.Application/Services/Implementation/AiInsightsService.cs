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
            return new BadRequestObjectResult("Production line has no camera.");

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

        if (_aiApiSettings.SkipInsightsApiCalls)
        {
            return new OkObjectResult(new OpenReportResponseDto
            {
                Status = "success",
                ReportId = report.Id.ToString(),
                Message = "Session started locally. AI Insights API call is skipped by configuration."
            });
        }

        var request = new
        {
            report_id = report.Id.ToString(),
            camera_source = productionLine.Camera.CameraSource,
            production_line_id = productionLine.Id.ToString(),
            target_speed = 150,
            max_temp = productionLine.MaximumTemperature,
            max_amps = productionLine.MaximumCurrent
        };

        var url = $"{_aiApiSettings.InsightsBaseUrl}/api/reports/open";
        OpenReportResponseDto? response;
        try
        {
            response = await externalApiService.PostAsync<OpenReportResponseDto>(url, request);
        }
        catch (HttpRequestException)
        {
            context.Reports.Remove(report);
            await context.SaveChangesAsync();
            return new ObjectResult("Failed to start session on AI service.") { StatusCode = 502 };
        }
        catch (TaskCanceledException)
        {
            context.Reports.Remove(report);
            await context.SaveChangesAsync();
            return new ObjectResult("Failed to start session on AI service.") { StatusCode = 502 };
        }

        if (response == null || response.Status != "success")
        {
            context.Reports.Remove(report);
            await context.SaveChangesAsync();
            return new ObjectResult(response?.Message ?? "Failed to start session on AI service.") { StatusCode = 502 };
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

        if (_aiApiSettings.SkipInsightsApiCalls)
        {
            report.EndedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return new OkObjectResult(new CloseSessionResponseDto
            {
                Status = "success",
                Message = "Session closed locally. AI Insights API call is skipped by configuration."
            });
        }

        var request = new
        {
            report_id = report.Id.ToString()
        };

        var url = $"{_aiApiSettings.InsightsBaseUrl}/api/reports/close";

        CloseSessionResponseDto? response;
        try
        {
            response = await externalApiService.PostAsync<CloseSessionResponseDto>(url, request);
        }
        catch (HttpRequestException)
        {
            return new ObjectResult("Failed to close session on AI service.") { StatusCode = 502 };
        }
        catch (TaskCanceledException)
        {
            return new ObjectResult("Failed to close session on AI service.") { StatusCode = 502 };
        }

        if (response != null && response.Status == "success" && report.EndedAt == null)
        {
            report.EndedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return response != null
            ? new OkObjectResult(response)
            : new ObjectResult("Failed to close session on AI service.") { StatusCode = 502 };
    }
}