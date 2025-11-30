using Chainly.Application.DTOs.ProductionLines;

namespace Chainly.Application.DTOs.Reports;

public class ReportDto
{
    public int ReportId { get; set; }
    public string VideoName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int DefectiveCount { get; set; }
    public int NonDefectiveCount { get; set; }
    public string ReportDate { get; set; }
    public ProductionLineDto ProductionLine { get; set; } = new();
    public List<DefectiveProductReportDto> DefectiveProducts { get; set; } = new();
}