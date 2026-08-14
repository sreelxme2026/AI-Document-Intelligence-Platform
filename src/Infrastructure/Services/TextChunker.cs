using Application.DTOs;
using Application.Interfaces;

namespace Infrastructure.Services;

public class TextChunker : ITextChunker
{
    private const int TargetTokenCount = 800;
    private const int OverlapTokenCount = 100;

    public IReadOnlyList<TextChunkResult> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var words = text
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        var approximateTokensPerWord = 0.75;

        var targetWordCount =
            Math.Max(
                1,
                (int)Math.Round(
                    TargetTokenCount /
                    approximateTokensPerWord));

        var overlapWordCount =
            Math.Max(
                0,
                (int)Math.Round(
                    OverlapTokenCount /
                    approximateTokensPerWord));

        var step =
            Math.Max(
                1,
                targetWordCount - overlapWordCount);

        var chunks = new List<TextChunkResult>();

        var chunkIndex = 0;

        for (
            var start = 0;
            start < words.Length;
            start += step)
        {
            var count = Math.Min(
                targetWordCount,
                words.Length - start);

            var chunkWords = words
                .Skip(start)
                .Take(count)
                .ToArray();

            var content = string.Join(
                " ",
                chunkWords);

            var tokenCount =
                (int)Math.Round(
                    chunkWords.Length *
                    approximateTokensPerWord);

            chunks.Add(new TextChunkResult
            {
                ChunkIndex = chunkIndex++,
                Content = content,
                TokenCount = tokenCount
            });

            if (start + count >= words.Length)
            {
                break;
            }
        }

        return chunks;
    }
}