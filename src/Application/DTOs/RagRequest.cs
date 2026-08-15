namespace Application.DTOs;

public class RagRequest
{
    public string Query { get; set; } = string.Empty;

    public int TopK { get; set; } = 5;
}