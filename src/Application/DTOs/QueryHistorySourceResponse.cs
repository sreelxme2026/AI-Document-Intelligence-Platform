namespace Application.DTOs;

public class QueryHistorySourceResponse
{
    public Guid DocumentChunkId { get; set; }

    public float RelevanceScore { get; set; }
}