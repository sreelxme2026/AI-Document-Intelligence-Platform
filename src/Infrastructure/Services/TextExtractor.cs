using Application.Interfaces;
using UglyToad.PdfPig;

namespace Infrastructure.Services;

public class TextExtractor : ITextExtractor
{
    public async Task<string> ExtractTextAsync(
        string filePath,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (contentType.Equals(
                "text/plain",
                StringComparison.OrdinalIgnoreCase))
        {
            return await File.ReadAllTextAsync(
                filePath,
                cancellationToken);
        }

        if (contentType.Equals(
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return ExtractPdfText(filePath);
        }

        throw new InvalidOperationException(
            $"Unsupported content type: {contentType}");
    }

    private static string ExtractPdfText(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        var pages = document
            .GetPages()
            .Select(page => page.Text);

        return string.Join(
            Environment.NewLine,
            pages);
    }
}