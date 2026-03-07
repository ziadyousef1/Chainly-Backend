namespace Chainly.Application.DTOs.Cameras;

public class CameraResponseDto
{
    public int Id { get; set; }
    public int ProductionLineId { get; set; }
    public string ProductionLineName { get; set; }
    public string CameraSource { get; set; }
}