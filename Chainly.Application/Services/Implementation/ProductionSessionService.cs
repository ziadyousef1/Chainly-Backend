using Chainly.Application.DTOs;
using Chainly.Application.DTOs.ProductionSession;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Chainly.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class ProductionSessionService(AppDbContext context) : IProductionSessionService
{
    public async Task<IActionResult> CreateSessionAsync(CreateProductionSessionDto request, int companyId)
    {
        var existingSession = await context.ProductionSessions
            .FirstOrDefaultAsync(s => 
                s.ProductId == request.ProductId && 
                s.CompanyId == companyId);

        if (existingSession != null)
        {
            existingSession.Status = request.Status;
            existingSession.MaxDefects = request.MaxDefects;
            existingSession.Timestamp = request.Timestamp;
            existingSession.ProductionLineId = request.ProductionLineId;

            context.ProductionSessions.Update(existingSession);
            await context.SaveChangesAsync();

            return new OkObjectResult(new
            {
                message = "Production session updated successfully",
            });
        }

        var session = new ProductionSession
        {
            ProductId = request.ProductId,
            SessionId = request.SessionId,
            Status = request.Status,
            MaxDefects = request.MaxDefects,
            Timestamp = request.Timestamp,
            ProductionLineId = request.ProductionLineId,
            CompanyId = companyId
        };

        context.ProductionSessions.Add(session);
        await context.SaveChangesAsync();

        return new OkObjectResult(new
        {
            message = "Production session created successfully",
        });
    }
}
