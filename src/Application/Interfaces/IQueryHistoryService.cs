using Application.DTOs;

namespace Application.Interfaces;

public interface IQueryHistoryService
{
    Task<QueryHistoryResponse> CreateAsync(
        Guid userId,
        string query,
        string answer,
        bool isGrounded,
        int? responseTimeMs,
        IReadOnlyList<QueryHistorySourceRequest> sources);

    Task<QueryHistoryListResponse> GetHistoryAsync(
        Guid userId,
        QueryHistoryQueryParameters parameters);

    Task<QueryHistoryResponse?> GetByIdAsync(
        Guid userId,
        Guid historyId);
}