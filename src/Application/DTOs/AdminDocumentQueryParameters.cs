namespace Application.DTOs;

public class AdminDocumentQueryParameters
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? Status { get; set; }

    public Guid? UploaderId { get; set; }
}