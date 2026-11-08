using Microsoft.AspNetCore.Http;
using RaceDayPOE.Services;

namespace RaceDayPOE.Tests;

//Image validation uses the real file signature, never the client-supplied content type.
public class ImageValidatorTests
{
    private static IFormFile FileOf(byte[] bytes, string name = "upload.bin") =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name);

    [Theory]
    [InlineData("png", "image/png", ".png")]
    [InlineData("jpeg", "image/jpeg", ".jpg")]
    [InlineData("gif", "image/gif", ".gif")]
    [InlineData("webp", "image/webp", ".webp")]
    public async Task Inspect_RecognisesSupportedImages(string kind, string contentType, string extension)
    {
        var bytes = kind switch { "png" => ImageFixtures.Png(), "jpeg" => ImageFixtures.Jpeg(), "gif" => ImageFixtures.Gif(), _ => ImageFixtures.WebP() };
        var result = await ImageValidator.InspectAsync(FileOf(bytes));
        Assert.True(result.IsValid);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(extension, result.Extension);
    }

    [Fact]
    public async Task Inspect_RejectsTextRenamedToPng()
    {
        var result = await ImageValidator.InspectAsync(FileOf(ImageFixtures.NotAnImage(), "fake.png"));
        Assert.False(result.IsValid);
        Assert.Contains("JPG, PNG, GIF or WebP", result.Error);
    }

    [Fact]
    public async Task Inspect_RejectsMissingAndEmptyFiles()
    {
        Assert.False((await ImageValidator.InspectAsync(null)).IsValid);
        Assert.False((await ImageValidator.InspectAsync(FileOf(Array.Empty<byte>()))).IsValid);
    }

    [Fact]
    public async Task Inspect_RejectsFilesShorterThanASignature()
    {
        Assert.False((await ImageValidator.InspectAsync(FileOf(new byte[] { 0xFF, 0xD8, 0xFF }))).IsValid);
    }

    [Fact]
    public async Task Inspect_RejectsFilesOverFiveMegabytes()
    {
        var bytes = new byte[ImageValidator.MaxBytes + 1];
        ImageFixtures.Png().CopyTo(bytes, 0);
        var result = await ImageValidator.InspectAsync(FileOf(bytes));
        Assert.False(result.IsValid);
        Assert.Contains("5 MB", result.Error);
    }
}
