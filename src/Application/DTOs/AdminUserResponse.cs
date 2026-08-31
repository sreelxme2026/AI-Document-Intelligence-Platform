namespace Application.DTOs;

public class AdminUserResponse
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}