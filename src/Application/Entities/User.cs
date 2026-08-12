using Application.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Entities;

public class User : IdentityUser<Guid>
{
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; }
}