using Application.DTOs;
using Infrastructure.Services;

namespace Tests;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void Chunk_EmptyText_ReturnsNoChunks()
    {
        var result = _chunker.Chunk(string.Empty);

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var text = "This is a short document.";

        var result = _chunker.Chunk(text);

        var chunk = Assert.Single(result);

        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal(text, chunk.Content);
        Assert.Equal(4, chunk.TokenCount);
    }

    [Fact]
    public void Chunk_LongText_CreatesMultipleChunks()
    {
        var words = CreateWords(1600);
        var text = string.Join(" ", words);

        var result = _chunker.Chunk(text);

        Assert.Equal(2, result.Count);

        Assert.Equal(0, result[0].ChunkIndex);
        Assert.Equal(1, result[1].ChunkIndex);
    }

    [Fact]
    public void Chunk_LongText_FirstChunkUsesApproximately800Tokens()
    {
        var words = CreateWords(1600);
        var text = string.Join(" ", words);

        var result = _chunker.Chunk(text);

        var firstChunkWords = result[0]
            .Content
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(1067, firstChunkWords.Length);
        Assert.Equal(800, result[0].TokenCount);
    }

    [Fact]
    public void Chunk_LongText_HasApproximately100TokenOverlap()
    {
        var words = CreateWords(1600);
        var text = string.Join(" ", words);

        var result = _chunker.Chunk(text);

        var firstChunkWords = result[0]
            .Content
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        var secondChunkWords = result[1]
            .Content
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries);

        var expectedOverlapWordCount = 133;

        var firstChunkOverlap = firstChunkWords
            .TakeLast(expectedOverlapWordCount)
            .ToArray();

        var secondChunkOverlap = secondChunkWords
            .Take(expectedOverlapWordCount)
            .ToArray();

        Assert.Equal(
            firstChunkOverlap,
            secondChunkOverlap);
    }

    [Fact]
    public void Chunk_LongText_ChunksHaveCorrectTokenCounts()
    {
        var words = CreateWords(1600);
        var text = string.Join(" ", words);

        var result = _chunker.Chunk(text);

        Assert.Equal(800, result[0].TokenCount);
        Assert.Equal(500, result[1].TokenCount);
    }

    private static string[] CreateWords(int count)
    {
        return Enumerable
            .Range(1, count)
            .Select(index => $"word{index}")
            .ToArray();
    }
}