namespace Application.Entities;

public class QueryHistorySource
{
    public Guid Id { get; set; }

    public Guid QueryHistoryId { get; set; }

    public Guid DocumentChunkId { get; set; }

    public float RelevanceScore { get; set; }
}