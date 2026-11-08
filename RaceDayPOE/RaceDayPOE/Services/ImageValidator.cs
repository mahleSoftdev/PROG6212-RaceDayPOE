namespace RaceDayPOE.Services;

//Validates uploaded images by size and by their real file signature, not by the client-supplied content type.
public static class ImageValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public sealed record Result(string? ContentType, string? Extension, string? Error)
    {
        public bool IsValid => Error is null;
    }

    public static async Task<Result> InspectAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0) return Invalid("Please choose an image file to upload.");
        if (file.Length > MaxBytes) return Invalid("The image must be 5 MB or smaller.");

        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
            read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read < header.Length) return Invalid("The file is not a valid image.");

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return new Result("image/jpeg", ".jpg", null);
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            return new Result("image/png", ".png", null);
        if (header[0] == 'G' && header[1] == 'I' && header[2] == 'F' && header[3] == '8') return new Result("image/gif", ".gif", null);
        if (header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F' && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P')
            return new Result("image/webp", ".webp", null);

        return Invalid("Only JPG, PNG, GIF or WebP images are allowed.");
    }

    private static Result Invalid(string message) => new(null, null, message);
}
