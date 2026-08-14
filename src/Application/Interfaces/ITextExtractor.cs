namespace Application.Interfaces;

public interface ITextExtractor
{
    Task<string> ExtractTextAsync(
        string filePath,
        string contentType,
        CancellationToken cancellationToken);
}