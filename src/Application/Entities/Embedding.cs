namespace Application.Entities;

public class Embedding
{
    public Guid Id { get; set; }

    public Guid DocumentChunkId { get; set; }

    public string VectorJson { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Dimension { get; set; }

    public DateTime CreatedAt { get; set; }
}