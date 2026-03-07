namespace Chainly.Domain.Models;

public class Camera
{
    public int Id { get; set; }

    public int ProductionLineId { get; set; }

    public ProductionLine ProductionLine { get; set; }
    
    public string CameraSource { get; set; }
}