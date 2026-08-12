namespace Application.Entities;

public class QueryHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string QueryText { get; set; } = string.Empty;

    public string? AnswerText { get; set; }

    public bool IsGrounded { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ResponseTimeMs { get; set; }
}