using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AdminQueryHistoryService : IAdminQueryHistoryService
{
    private readonly AppDbContext _dbContext;

    public AdminQueryHistoryService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QueryHistoryListResponse> GetHistoryAsync(
        AdminQueryHistoryQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var page = parameters.Page <= 0
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize <= 0
            ? 10
            : Math.Min(parameters.PageSize, 100);

        var query = _dbContext.QueryHistories
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search
                .Trim()
                .ToLower();

            query = query.Where(history =>
                history.QueryText
                    .ToLower()
                    .Contains(search));
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(history =>
                history.UserId == parameters.UserId.Value);
        }

        if (parameters.FromDate.HasValue)
        {
            query = query.Where(history =>
                history.CreatedAt >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(history =>
                history.CreatedAt <= parameters.ToDate.Value);
        }

        var totalCount = await query.CountAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);

        var histories = await query
            .OrderByDescending(history => history.CreatedAt)
            .ThenByDescending(history => history.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var historyIds = histories
            .Select(history => history.Id)
            .ToList();

        var sources = await _dbContext.QueryHistorySources
            .AsNoTracking()
            .Where(source =>
                historyIds.Contains(source.QueryHistoryId))
            .ToListAsync();

        var sourcesByHistory = sources
            .GroupBy(source => source.QueryHistoryId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<QueryHistorySourceResponse>)
                    group.Select(source =>
                        new QueryHistorySourceResponse
                        {
                            DocumentChunkId =
                                source.DocumentChunkId,

                            RelevanceScore =
                                source.RelevanceScore
                        })
                    .ToList());

        var items = histories
            .Select(history =>
                new QueryHistoryResponse
                {
                    Id = history.Id,

                    UserId = history.UserId,

                    Query = history.QueryText,

                    Answer = history.AnswerText
                        ?? string.Empty,

                    IsGrounded = history.IsGrounded,

                    CreatedAt = history.CreatedAt,

                    ResponseTimeMs =
                        history.ResponseTimeMs,

                    Sources =
                        sourcesByHistory.TryGetValue(
                            history.Id,
                            out var historySources)
                            ? historySources
                            : []
                })
            .ToList();

        return new QueryHistoryListResponse
        {
            Items = items,

            Page = page,

            PageSize = pageSize,

            TotalCount = totalCount,

            TotalPages = totalPages
        };
    }

    public async Task<QueryHistoryResponse?> GetByIdAsync(
        Guid historyId)
    {
        if (historyId == Guid.Empty)
        {
            throw new ArgumentException(
                "History ID cannot be empty.",
                nameof(historyId));
        }

        var history = await _dbContext.QueryHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(history =>
                history.Id == historyId);

        if (history is null)
        {
            return null;
        }

        var sources = await _dbContext.QueryHistorySources
            .AsNoTracking()
            .Where(source =>
                source.QueryHistoryId == historyId)
            .Select(source =>
                new QueryHistorySourceResponse
                {
                    DocumentChunkId =
                        source.DocumentChunkId,

                    RelevanceScore =
                        source.RelevanceScore
                })
            .ToListAsync();

        return new QueryHistoryResponse
        {
            Id = history.Id,

            UserId = history.UserId,

            Query = history.QueryText,

            Answer = history.AnswerText
                ?? string.Empty,

            IsGrounded = history.IsGrounded,

            CreatedAt = history.CreatedAt,

            ResponseTimeMs =
                history.ResponseTimeMs,

            Sources = sources
        };
    }
}