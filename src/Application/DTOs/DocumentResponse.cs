namespace Application.DTOs;

public class DocumentResponse
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Guid UploadedByUserId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int? PageCount { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Tags { get; set; }
}