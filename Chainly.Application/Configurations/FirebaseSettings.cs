namespace Chainly.Application.Configurations;

public class FirebaseSettings
{
    public string DatabaseUrl { get; set; } = string.Empty;
    public string? ServiceAccountKeyPath { get; set; }
    public string? ServiceAccountJson { get; set; }
}
