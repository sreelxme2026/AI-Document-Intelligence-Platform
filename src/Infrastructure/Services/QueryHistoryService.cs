using Application.DTOs;
using Application.Entities;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class QueryHistoryService : IQueryHistoryService
{
    private readonly AppDbContext _dbContext;

    public QueryHistoryService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QueryHistoryResponse> CreateAsync(
        Guid userId,
        string query,
        string answer,
        bool isGrounded,
        int? responseTimeMs,
        IReadOnlyList<QueryHistorySourceRequest> sources)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Query cannot be null or empty.",
                nameof(query));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException(
                "Answer cannot be null or empty.",
                nameof(answer));
        }

        if (responseTimeMs < 0)
        {
            throw new ArgumentException(
                "Response time cannot be negative.",
                nameof(responseTimeMs));
        }

        if (sources is null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = query.Trim(),
            AnswerText = answer.Trim(),
            IsGrounded = isGrounded,
            ResponseTimeMs = responseTimeMs,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.QueryHistories.Add(history);

        foreach (var source in sources)
        {
            if (source.DocumentChunkId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Source document chunk ID cannot be empty.",
                    nameof(sources));
            }

            if (source.RelevanceScore < 0)
            {
                throw new ArgumentException(
                    "Source relevance score cannot be negative.",
                    nameof(sources));
            }

            var historySource = new QueryHistorySource
            {
                Id = Guid.NewGuid(),
                QueryHistoryId = history.Id,
                DocumentChunkId = source.DocumentChunkId,
                RelevanceScore = source.RelevanceScore
            };

            _dbContext.QueryHistorySources.Add(historySource);
        }

        await _dbContext.SaveChangesAsync();

        var responseSources = sources
            .Select(source =>
                new QueryHistorySourceResponse
                {
                    DocumentChunkId = source.DocumentChunkId,
                    RelevanceScore = source.RelevanceScore
                })
            .ToList();

        return MapToResponse(
            history,
            responseSources);
    }

    public async Task<QueryHistoryListResponse> GetHistoryAsync(
        Guid userId,
        QueryHistoryQueryParameters parameters)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var page = parameters.Page <= 0
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize <= 0
            ? 10
            : parameters.PageSize;

        var query = _dbContext.QueryHistories
            .AsNoTracking()
            .Where(history => history.UserId == userId);

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
                group => (IReadOnlyList<QueryHistorySourceResponse>)
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
                MapToResponse(
                    history,
                    sourcesByHistory.TryGetValue(
                        history.Id,
                        out var historySources)
                        ? historySources
                        : []))
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
        Guid userId,
        Guid historyId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        if (historyId == Guid.Empty)
        {
            throw new ArgumentException(
                "History ID cannot be empty.",
                nameof(historyId));
        }

        var history = await _dbContext.QueryHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == historyId &&
                    item.UserId == userId);

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

        return MapToResponse(
            history,
            sources);
    }

    private static QueryHistoryResponse MapToResponse(
        QueryHistory history,
        IReadOnlyList<QueryHistorySourceResponse> sources)
    {
        return new QueryHistoryResponse
        {
            Id = history.Id,
            UserId = history.UserId,
            Query = history.QueryText,
            Answer = history.AnswerText ?? string.Empty,
            IsGrounded = history.IsGrounded,
            CreatedAt = history.CreatedAt,
            ResponseTimeMs = history.ResponseTimeMs,
            Sources = sources
        };
    }
}