using System.Text.Json.Serialization;

namespace Chainly.Application.Dtos;

public class LoginResponseDto
{
    public string Token { get; set; }
    [JsonIgnore]
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}