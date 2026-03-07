using Chainly.Application.DTOs.Cameras;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class CameraService(AppDbContext context) : ICameraService
{
    public async Task<IActionResult> GetCamerasAsync(int companyId, int? productionLineId = null, string? search = null,
        int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = context.Cameras
            .Include(c => c.ProductionLine)
            .AsNoTracking()
            .Where(c => c.ProductionLine.CompanyId == companyId);

        if (productionLineId.HasValue)
            query = query.Where(c => c.ProductionLineId == productionLineId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(c =>
                c.CameraSource.ToLower().Contains(search) ||
                c.ProductionLine.LineName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CameraResponseDto
            {
                Id = c.Id,
                ProductionLineId = c.ProductionLineId,
                ProductionLineName = c.ProductionLine.LineName,
                CameraSource = c.CameraSource
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

    public async Task<IActionResult> CreateCameraAsync(int companyId, CreateCameraDto dto)
    {
        var productionLine = await context.ProductionLines
            .Include(p => p.Camera)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId && p.CompanyId == companyId);

        if (productionLine == null)
            return new NotFoundObjectResult("Production line not found for this company.");

        if (productionLine.Camera != null)
            return new BadRequestObjectResult("This production line already has a camera.");

        var camera = new Camera
        {
            ProductionLineId = dto.ProductionLineId,
            CameraSource = dto.CameraSource
        };

        await context.Cameras.AddAsync(camera);
        await context.SaveChangesAsync();

        return new OkObjectResult(new CameraResponseDto
        {
            Id = camera.Id,
            ProductionLineId = camera.ProductionLineId,
            ProductionLineName = productionLine.LineName,
            CameraSource = camera.CameraSource
        });
    }

    public async Task<IActionResult> UpdateCameraAsync(int companyId, int cameraId, UpdateCameraDto dto)
    {
        var camera = await context.Cameras
            .Include(c => c.ProductionLine)
            .FirstOrDefaultAsync(c => c.Id == cameraId && c.ProductionLine.CompanyId == companyId);

        if (camera == null)
            return new NotFoundObjectResult("Camera not found for this company.");

        if (!string.IsNullOrWhiteSpace(dto.CameraSource))
            camera.CameraSource = dto.CameraSource;

        if (dto.ProductionLineId.HasValue && dto.ProductionLineId.Value != camera.ProductionLineId)
        {
            var newLine = await context.ProductionLines
                .Include(p => p.Camera)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductionLineId.Value && p.CompanyId == companyId);

            if (newLine == null)
                return new NotFoundObjectResult("New production line not found for this company.");

            if (newLine.Camera != null)
                return new BadRequestObjectResult("This production line already has a camera.");

            camera.ProductionLineId = dto.ProductionLineId.Value;
        }

        await context.SaveChangesAsync();

        return new OkObjectResult(new CameraResponseDto
        {
            Id = camera.Id,
            ProductionLineId = camera.ProductionLineId,
            ProductionLineName = camera.ProductionLine.LineName,
            CameraSource = camera.CameraSource
        });
    }

    public async Task<IActionResult> DeleteCameraAsync(int companyId, int cameraId)
    {
        var camera = await context.Cameras
            .Include(c => c.ProductionLine)
            .FirstOrDefaultAsync(c => c.Id == cameraId && c.ProductionLine.CompanyId == companyId);

        if (camera == null)
            return new NotFoundObjectResult("Camera not found for this company.");

        context.Cameras.Remove(camera);
        await context.SaveChangesAsync();

        return new OkObjectResult(new { message = "Camera deleted successfully." });
    }
}