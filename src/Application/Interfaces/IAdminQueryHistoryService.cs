using Application.DTOs;

namespace Application.Interfaces;

public interface IAdminQueryHistoryService
{
    Task<QueryHistoryListResponse> GetHistoryAsync(
        AdminQueryHistoryQueryParameters parameters);

    Task<QueryHistoryResponse?> GetByIdAsync(
        Guid historyId);
}