using Microsoft.AspNetCore.Mvc;

namespace Chainly.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PredictionsController : Controller
{
    [HttpPost("get-video-predictions")]
    public async Task<IActionResult> Index(IFormFile file)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://20.51.117.96:8000/process-defect-video/");
        var content = new MultipartFormDataContent();

        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        request.Content = content;

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseContent);

        return Ok(responseContent);
    }
    [HttpGet("download-video/{sessionId}")]
    public async Task<IActionResult> DownloadVideo(string sessionId)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://20.51.117.96:8000/download-video/{sessionId}");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var videoContent = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = "downloaded-video.mp4";

        return File(videoContent, contentType, fileName);
    }
    [HttpGet("processing-status/{sessionId}")]
    public async Task<IActionResult> CheckProcessingStatus(string sessionId)
    {
        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://20.51.117.96:8000/processing-status/{sessionId}");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var statusContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine(statusContent);

        return Ok(statusContent);
    }
    
}