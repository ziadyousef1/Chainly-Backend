namespace Chainly.Application.DTOs;

public class GeoapifyResponse
{
    public List<Feature> Features { get; set; }

    public class Feature
    {
        public Properties Properties { get; set; }
    }

    public class Properties
    {
        public string Formatted { get; set; }
    } 
}