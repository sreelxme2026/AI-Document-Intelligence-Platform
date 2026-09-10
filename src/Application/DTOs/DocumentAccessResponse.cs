namespace Application.DTOs;

public class DocumentAccessResponse
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }

    public DateTime GrantedAt { get; set; }
}