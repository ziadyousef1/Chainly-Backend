namespace Chainly.Application.DTOs.Reports;

public class ReportSummaryDto
{
    public int ProductionLineId { get; set; }
    public int TotalProducts { get; set; }
    public int DefectiveProductsCount { get; set; }
    public int NonDefectiveProductsCount { get; set; }
    public int TotalDefectiveItems { get; set; }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    public List<DefectiveProductReportDto> DefectiveProducts { get; set; } = new();
}