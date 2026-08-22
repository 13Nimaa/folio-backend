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

    byte[] imageBytes;

    try
    {
        imageBytes = Convert.FromBase64String(base64Data);
    }
    catch (FormatException)
    {
        throw new BadHttpRequestException("Cover image is not valid base64.");
    }

    // 5 MB ceiling on decoded content so an authenticated caller cannot
    // push arbitrary multi-megabyte blobs to Cloudinary.
    if (imageBytes.Length > MaxImageBytes)
    {
        throw new BadHttpRequestException(
            $"Cover image exceeds the {MaxImageBytes / (1024 * 1024)} MB limit.");
    }

    EnsureIsSupportedImage(imageBytes);

    using var stream = new MemoryStream(imageBytes);

    var uploadParams = new ImageUploadParams
    {
        File = new FileDescription(
            "book-cover",
            stream)
    };

    var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

    if (result.Error is not null)
    {
        throw new InvalidOperationException(
            result.Error.Message);
    }

    return result.SecureUrl.ToString();
}

    private const int MaxImageBytes = 5 * 1024 * 1024;

    // Sniff magic bytes: never trust a declared extension or data-URL prefix.
    private static void EnsureIsSupportedImage(byte[] bytes)
    {
        bool isJpeg = bytes.Length >= 3 &&
            bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        bool isPng = bytes.Length >= 4 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 &&
            bytes[2] == 0x4E && bytes[3] == 0x47;
        bool isWebP = bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' &&
            bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' &&
            bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

        if (!isJpeg && !isPng && !isWebP)
        {
            throw new BadHttpRequestException(
                "Cover image must be a JPG, PNG, or WebP file.");
        }
    }
}