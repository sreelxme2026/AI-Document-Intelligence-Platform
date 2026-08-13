using Application.Exceptions;
using Application.Interfaces;

namespace Infrastructure.Services;

public class FileValidator : IFileValidator
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "text/plain"
    ];

    public void Validate(
        string fileName,
        string contentType,
        long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidFileException("File name is required.");
        }

        if (fileSize <= 0)
        {
            throw new InvalidFileException("File cannot be empty.");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            throw new InvalidFileException(
                "File size cannot exceed 25 MB.");
        }

        if (!AllowedContentTypes.Contains(
                contentType.ToLowerInvariant()))
        {
            throw new InvalidFileException(
                "Only PDF and TXT files are supported.");
        }
    }
}