using Chainly.Application.DTOs.Simulation;
using Chainly.Application.Services.Interfaces;
using Chainly.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chainly.Application.Services.Implementation;

public class SimulationService(AppDbContext context, IAzureOpenAIService azureOpenAiService) : ISimulationService
{
    public async Task<IActionResult> SimulateAsync(SimulationRequestDto dto, int companyId)
    {
        double maxSpeed, ratedPower, maxTemperature;

        if (dto.ProductionLineId.HasValue)
        {
            var line = await context.ProductionLines
                .FirstOrDefaultAsync(l =>
                    l.Id == dto.ProductionLineId &&
                    l.CompanyId == companyId);

            if (line == null)
                return new NotFoundObjectResult("Production line not found.");

            maxSpeed = line.MaximumSpeed;
            ratedPower = line.RatedPower;
            maxTemperature = line.MaximumTemperature;
        }
        else
        {
            if (!dto.MaximumSpeed.HasValue ||
                !dto.RatedPower.HasValue ||
                !dto.MaximumTemperature.HasValue)
            {
                return new BadRequestObjectResult("Configuration values are required when ProductionLineId is not selected.");
            }

            maxSpeed = dto.MaximumSpeed.Value;
            ratedPower = dto.RatedPower.Value;
            maxTemperature = dto.MaximumTemperature.Value;
        }

        double effectiveThroughput = Math.Min(dto.Throughput, maxSpeed);

        if (effectiveThroughput <= 0 || dto.OperatingTime <= 0)
            return new BadRequestObjectResult("Throughput and operating time must be greater than zero.");

        double load = dto.Demand / (effectiveThroughput * dto.OperatingTime);

        double production = effectiveThroughput * dto.OperatingTime;

        double T = dto.EnvironmentalTemperature;
        double heatIn = ratedPower * load;
        double heatOut = T - dto.EnvironmentalTemperature;
        double temperature = T + heatIn - heatOut;

        bool isOk = temperature <= maxTemperature && production >= dto.Demand;

        var result = new
        {
            EffectiveThroughput = Math.Round(effectiveThroughput, 2),
            Load = Math.Round(load, 2),
            Production = Math.Round(production, 2),
            Temperature = Math.Round(temperature, 2),
            IsOk = isOk
        };

        var aiAnalysis = await azureOpenAiService
            .GenerateSimulationAnalysisAsync(result);

        return new OkObjectResult(new
        {
            Simulation = result,
            AIAnalysis = aiAnalysis
        });
    }
}