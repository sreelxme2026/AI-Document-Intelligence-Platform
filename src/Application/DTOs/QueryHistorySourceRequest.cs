namespace Application.DTOs;

public class QueryHistorySourceRequest
{
    public Guid DocumentChunkId { get; set; }

    public float RelevanceScore { get; set; }
}