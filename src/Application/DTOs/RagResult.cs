namespace Application.DTOs;

public class RagResult
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<RetrievalSource> Sources { get; set; }
        = [];
}