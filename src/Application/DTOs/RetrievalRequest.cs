namespace Application.DTOs;

public class RetrievalRequest
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;

    public Guid? UserId { get; set; }
}