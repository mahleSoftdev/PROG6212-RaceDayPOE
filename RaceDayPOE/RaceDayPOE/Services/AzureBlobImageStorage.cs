using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDayPOE.Services;

//Azure Blob Storage implementation of IImageStorage.
public sealed class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobStorageOptions _options;
    private readonly ILogger<AzureBlobImageStorage> _logger;

    public AzureBlobImageStorage(IOptions<BlobStorageOptions> options, ILogger<AzureBlobImageStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> UploadAsync(string folder, Stream content, string contentType, string extension, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new ImageStorageUnavailableException("Image storage is not configured on the server.");
        try
        {
            var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
            var blob = container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
            await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } }, cancellationToken);
            return blob.Uri.ToString();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Uploading an image to Azure Blob Storage failed.");
            throw new ImageStorageUnavailableException("Image storage is temporarily unavailable. Please try again later.", ex);
        }
    }

    public async Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(_options.ConnectionString)) return;
        try
        {
            var path = Uri.UnescapeDataString(new Uri(url).AbsolutePath);
            var marker = "/" + _options.ContainerName + "/";
            var index = path.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0) return;
            var container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
            await container.GetBlobClient(path[(index + marker.Length)..]).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not delete the old image from Azure Blob Storage.");
        }
    }
}
