using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Chainly.Application.Configurations;
using Chainly.Application.DTOs.CloudStorage;
using Chainly.Application.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Chainly.Application.Services.Implementation;

public class CloudStorageService : ICloudStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public CloudStorageService(IOptions<CloudStorageSettings> azureSettings)
    {
        var cloudSettings = azureSettings.Value;
        _blobServiceClient = new BlobServiceClient(cloudSettings.ConnectionString);
    }

    public async Task<BlobResponseDto> UploadAsync(string containerName, IFormFile blob, string prefix)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{prefix}-{blob.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = blob.OpenReadStream();

        var headers = new BlobHttpHeaders
        {
            ContentType = blob.ContentType
        };

        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

        return new BlobResponseDto()
        {
            Status = "Uploaded successfully",
            Error = false,
            Blob = new BlobDto()
            {
                Name = blobName,
                Uri = blobClient.Uri.AbsoluteUri,
            }
        };
    }

    public async Task<BlobResponseDto> DeleteAsync(string containerName, string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
        return new BlobResponseDto()
        {
            Status = "Deleted successfully",
            Error = false
        };
    }

    public async Task<BlobDto?> DownloadAsync(string containerName, string blobFileName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobFileName);

        if (!await blobClient.ExistsAsync())
            return null;

        var downloadResponse = await blobClient.DownloadContentAsync();
        var contentType = downloadResponse.Value.Details.ContentType;

        return new BlobDto
        {
            Name = blobFileName,
            Content = downloadResponse.Value.Content.ToStream(),
            ContentType = contentType
        };
    }
}