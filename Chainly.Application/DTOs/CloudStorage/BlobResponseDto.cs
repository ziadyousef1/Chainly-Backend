namespace Chainly.Application.DTOs.CloudStorage;

public class BlobResponseDto
{
    public string? Status { get; set; }
    public bool Error { get; set; }
    public BlobDto Blob { get; set; } = new();
}