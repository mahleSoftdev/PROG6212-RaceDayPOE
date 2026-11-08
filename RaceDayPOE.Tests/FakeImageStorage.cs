using RaceDayPOE.Services;

namespace RaceDayPOE.Tests;

//In-memory replacement for Azure Blob Storage so tests never touch the network.
public sealed class FakeImageStorage : IImageStorage
{
    private readonly object _lock = new();
    public bool Unavailable { get; set; }
    public List<string> Uploaded { get; } = new();
    public List<string> Deleted { get; } = new();

    public Task<string> UploadAsync(string folder, Stream content, string contentType, string extension, CancellationToken cancellationToken = default)
    {
        if (Unavailable) throw new ImageStorageUnavailableException("Image storage is temporarily unavailable.");
        var url = $"https://fake.blob.local/raceday-images/{folder}/{Guid.NewGuid():N}{extension}";
        lock (_lock) Uploaded.Add(url);
        return Task.FromResult(url);
    }

    public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (url is not null) lock (_lock) Deleted.Add(url);
        return Task.CompletedTask;
    }
}

//Byte sequences that start with real image signatures.
internal static class ImageFixtures
{
    private static byte[] Pad(byte[] head, int length = 64) { var bytes = new byte[length]; head.CopyTo(bytes, 0); return bytes; }
    public static byte[] Png() => Pad(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    public static byte[] Jpeg() => Pad(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
    public static byte[] Gif() => Pad("GIF89a"u8.ToArray());
    public static byte[] WebP() => Pad("RIFF"u8.ToArray().Concat(new byte[] { 0, 0, 0, 0 }).Concat("WEBP"u8.ToArray()).ToArray());
    public static byte[] NotAnImage() => Pad("This is plain text, not an image."u8.ToArray());

    public static MultipartFormDataContent Upload(byte[] bytes, string fileName = "image.png", string contentType = "image/png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }
}
