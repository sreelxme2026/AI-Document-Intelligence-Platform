namespace Application.DTOs;

public class DocumentQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Status { get; set; }

    public Guid? UploaderId { get; set; }
}