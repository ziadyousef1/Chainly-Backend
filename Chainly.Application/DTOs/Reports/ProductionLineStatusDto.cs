namespace Chainly.Application.DTOs.Reports;

public class ProductionLineStatusDto
{
    public int ProductionLineId { get; set; }
    public string ProductionLineName { get; set; }
    public string ProductionLineDescription { get; set; } = string.Empty;
    public bool Active { get; set; }
    public int? ReportId { get; set; }
}