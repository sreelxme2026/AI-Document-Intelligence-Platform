namespace Application.DTOs;

public class DocumentStatusResponse
{
    public Guid Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public DateTime? ProcessedAt { get; set; }
}