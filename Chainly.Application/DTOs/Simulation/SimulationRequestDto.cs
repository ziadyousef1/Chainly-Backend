namespace Chainly.Application.DTOs.Simulation;

public class SimulationRequestDto
{
    public int? ProductionLineId { get; set; }
    public double? MaximumSpeed { get; set; }    
    public double? RatedPower { get; set; }      
    public double? MaximumTemperature { get; set; } 
    public double Throughput { get; set; }     
    public double EnvironmentalTemperature { get; set; } 
    public double OperatingTime { get; set; }     
    public double Demand { get; set; }            
}