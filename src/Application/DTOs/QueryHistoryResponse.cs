namespace Application.DTOs;

public class QueryHistoryResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Query { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}