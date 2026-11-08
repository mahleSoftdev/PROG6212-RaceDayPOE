namespace RaceDayPOE.Services;

//Stores and removes uploaded images. The MVC front end never talks to storage directly: it uploads to the API.
public interface IImageStorage
{
    //Saves an image under the given folder and returns its public URL.
    Task<string> UploadAsync(string folder, Stream content, string contentType, string extension, CancellationToken cancellationToken = default);

    //Removes a previously uploaded image. Missing blobs and storage errors are ignored (best-effort clean-up).
    Task DeleteAsync(string? url, CancellationToken cancellationToken = default);
}

//Thrown when image storage is not configured or cannot be reached. Controllers translate it into HTTP 503.
public sealed class ImageStorageUnavailableException : Exception
{
    public ImageStorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
