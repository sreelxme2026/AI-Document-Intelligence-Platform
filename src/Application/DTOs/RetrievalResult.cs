namespace Application.DTOs;

public class RetrievalResult
{
    public IReadOnlyList<RetrievalSource> Sources { get; set; }
        = [];
}