namespace Application.Entities;

public class DocumentAccess
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public Guid UserId { get; set; }

    public DateTime GrantedAt { get; set; }
}