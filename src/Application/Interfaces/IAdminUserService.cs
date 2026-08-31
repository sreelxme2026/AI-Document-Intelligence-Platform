using Application.DTOs;

namespace Application.Interfaces;

public interface IAdminUserService
{
    Task<AdminUserListResponse> GetUsersAsync(
        AdminUserQueryParameters parameters);

    Task<bool> DeleteAsync(
        Guid userId);
}