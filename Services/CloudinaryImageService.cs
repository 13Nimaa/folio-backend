using BooksProject.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace BooksProject.Services;

public sealed class CloudinaryImageService : IImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(
        IOptions<CloudinarySettings> settings)
    {
        var account = new Account(
            settings.Value.CloudName,
            settings.Value.ApiKey,
            settings.Value.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

   public async Task<string> UploadBase64ImageAsync(
    string base64Image,
    CancellationToken cancellationToken = default)
{
    var base64Data = base64Image;

    if (base64Image.Contains(','))
    {
        base64Data = base64Image[(base64Image.IndexOf(',') + 1)..];
    }

    var imageBytes = Convert.FromBase64String(base64Data);

    using var stream = new MemoryStream(imageBytes);

    var uploadParams = new ImageUploadParams
    {
        File = new FileDescription(
            "book-cover",
            stream)
    };

    var result = await _cloudinary.UploadAsync(uploadParams);

    if (result.Error is not null)
    {
        throw new InvalidOperationException(
            result.Error.Message);
    }

    return result.SecureUrl.ToString();
}
}