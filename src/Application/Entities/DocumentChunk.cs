namespace Application.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public int TokenCount { get; set; }

    public DateTime CreatedAt { get; set; }
}