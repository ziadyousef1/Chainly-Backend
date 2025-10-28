using Chainly.Application.DTOs.CloudStorage;
using Microsoft.AspNetCore.Http;

namespace Chainly.Application.Services.Interfaces;

public interface ICloudStorageService
{
    Task<BlobResponseDto> UploadAsync(string containerName, IFormFile blob, string prefix);
    Task<BlobDto?> DownloadAsync(string containerName, string blobFileName);
    Task<BlobResponseDto> DeleteAsync(string containerName, string blobName);
}