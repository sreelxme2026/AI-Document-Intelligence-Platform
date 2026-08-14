namespace Application.DTOs;

public class TextChunkResult
{
    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int TokenCount { get; set; }
}