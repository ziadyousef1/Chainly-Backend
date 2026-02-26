using Chainly.Application.DTOs.ProductionLines;

namespace Chainly.Application.DTOs.Reports;

public class ReportDto
{
    public int ReportId { get; set; }
    public int TotalProducts { get; set; }
    public int GoodProducts { get; set; }
    public int DefectiveProducts { get; set; }
    public double GoodRatio { get; set; }
    public double DefectiveRatio { get; set; }
    public string StartedAt { get; set; }
    public string? EndedAt { get; set; }
    public int ProductionLineId { get; set; }
    public string ProductionLineName { get; set; }
}