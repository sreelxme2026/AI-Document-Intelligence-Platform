namespace Application.DTOs;

public class RetrievalSource
{
    public Guid DocumentChunkId { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public double SimilarityScore { get; set; }
}