namespace Chainly.Application.DTOs.Reports;

public class CreateVideoReportDto
{
    public int ProductionLineId { get; set; }
    public int GoodProducts { get; set; }
    public int DefectiveProducts { get; set; }
    public string VideoName { get; set; }
}