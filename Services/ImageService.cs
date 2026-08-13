namespace BooksProject.Services;

public interface IImageService
{
    Task<string> UploadBase64ImageAsync(
        string base64Image,
        CancellationToken cancellationToken = default);
}