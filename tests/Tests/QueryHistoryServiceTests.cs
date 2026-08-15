using Application.DTOs;
using Application.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public class QueryHistoryServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesHistory()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            userId,
            "What is the refund policy?",
            "Refunds are available within 30 days.",
            true,
            425,
            []);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(
            "What is the refund policy?",
            result.Query);
        Assert.Equal(
            "Refunds are available within 30 days.",
            result.Answer);
        Assert.True(result.IsGrounded);
        Assert.Equal(425, result.ResponseTimeMs);
        Assert.Empty(result.Sources);

        var stored = await dbContext.QueryHistories
            .SingleAsync();

        Assert.Equal(result.Id, stored.Id);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(
            "What is the refund policy?",
            stored.QueryText);
        Assert.Equal(
            "Refunds are available within 30 days.",
            stored.AnswerText);
        Assert.True(stored.IsGrounded);
        Assert.Equal(425, stored.ResponseTimeMs);
        Assert.NotEqual(default, stored.CreatedAt);
    }

    [Fact]
    public async Task CreateAsync_ValidSources_PersistsSources()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var firstChunkId = Guid.NewGuid();
        var secondChunkId = Guid.NewGuid();

        var sources = new List<QueryHistorySourceRequest>
        {
            new()
            {
                DocumentChunkId = firstChunkId,
                RelevanceScore = 0.95f
            },
            new()
            {
                DocumentChunkId = secondChunkId,
                RelevanceScore = 0.82f
            }
        };

        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            userId,
            "What is the leave policy?",
            "The leave policy allows 20 days.",
            true,
            300,
            sources);

        Assert.Equal(2, result.Sources.Count);

        var storedSources = await dbContext.QueryHistorySources
            .Where(source =>
                source.QueryHistoryId == result.Id)
            .OrderBy(source => source.DocumentChunkId)
            .ToListAsync();

        Assert.Equal(2, storedSources.Count);

        Assert.Contains(
            storedSources,
            source =>
                source.DocumentChunkId == firstChunkId &&
                source.RelevanceScore == 0.95f);

        Assert.Contains(
            storedSources,
            source =>
                source.DocumentChunkId == secondChunkId &&
                source.RelevanceScore == 0.82f);
    }

    [Fact]
    public async Task CreateAsync_PersistsGroundingAndResponseTime()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        var service = CreateService(dbContext);

        var result = await service.CreateAsync(
            userId,
            "Test query",
            "Test answer",
            false,
            1250,
            []);

        Assert.False(result.IsGrounded);
        Assert.Equal(1250, result.ResponseTimeMs);

        var stored = await dbContext.QueryHistories
            .SingleAsync();

        Assert.False(stored.IsGrounded);
        Assert.Equal(1250, stored.ResponseTimeMs);
    }

    [Fact]
    public async Task CreateAsync_EmptyUserId_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.Empty,
                "Test query",
                "Test answer",
                true,
                100,
                []));
    }

    [Fact]
    public async Task CreateAsync_EmptyQuery_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                "",
                "Test answer",
                true,
                100,
                []));
    }

    [Fact]
    public async Task CreateAsync_EmptyAnswer_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                "Test query",
                "",
                true,
                100,
                []));
    }

    [Fact]
    public async Task CreateAsync_NegativeResponseTime_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                "Test query",
                "Test answer",
                true,
                -1,
                []));
    }

    [Fact]
    public async Task CreateAsync_NullSources_ThrowsArgumentNullException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                "Test query",
                "Test answer",
                true,
                100,
                null!));
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOnlyUsersOwnHistory()
    {
        await using var dbContext = CreateDbContext();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var service = CreateService(dbContext);

        await service.CreateAsync(
            userA,
            "User A query",
            "User A answer",
            true,
            100,
            []);

        await service.CreateAsync(
            userB,
            "User B query",
            "User B answer",
            true,
            200,
            []);

        var result = await service.GetHistoryAsync(
            userA,
            new QueryHistoryQueryParameters());

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);

        Assert.Equal(
            "User A query",
            result.Items[0].Query);

        Assert.Equal(
            userA,
            result.Items[0].UserId);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsNewestFirst()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        var older = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = "Older query",
            AnswerText = "Older answer",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var newer = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = "Newer query",
            AnswerText = "Newer answer",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.QueryHistories.AddRange(
            older,
            newer);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            userId,
            new QueryHistoryQueryParameters());

        Assert.Equal(2, result.Items.Count);

        Assert.Equal(
            "Newer query",
            result.Items[0].Query);

        Assert.Equal(
            "Older query",
            result.Items[1].Query);
    }

    [Fact]
    public async Task GetHistoryAsync_AppliesPagination()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        for (var i = 1; i <= 5; i++)
        {
            dbContext.QueryHistories.Add(
                new QueryHistory
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    QueryText = $"Query {i}",
                    AnswerText = $"Answer {i}",
                    IsGrounded = true,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
        }

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            userId,
            new QueryHistoryQueryParameters
            {
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsSourcesForHistoryItems()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = "What is the policy?",
            AnswerText = "The policy says...",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.QueryHistories.Add(history);

        dbContext.QueryHistorySources.Add(
            new QueryHistorySource
            {
                Id = Guid.NewGuid(),
                QueryHistoryId = history.Id,
                DocumentChunkId = chunkId,
                RelevanceScore = 0.91f
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            userId,
            new QueryHistoryQueryParameters());

        Assert.Single(result.Items);
        Assert.Single(result.Items[0].Sources);

        Assert.Equal(
            chunkId,
            result.Items[0].Sources[0].DocumentChunkId);

        Assert.Equal(
            0.91f,
            result.Items[0].Sources[0].RelevanceScore);
    }

    [Fact]
    public async Task GetHistoryAsync_EmptyHistory_ReturnsEmptyResult()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            Guid.NewGuid(),
            new QueryHistoryQueryParameters());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUsersOwnHistory()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = "Test query",
            AnswerText = "Test answer",
            IsGrounded = true,
            ResponseTimeMs = 250,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.QueryHistories.Add(history);

        dbContext.QueryHistorySources.Add(
            new QueryHistorySource
            {
                Id = Guid.NewGuid(),
                QueryHistoryId = history.Id,
                DocumentChunkId = chunkId,
                RelevanceScore = 0.88f
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(
            userId,
            history.Id);

        Assert.NotNull(result);

        Assert.Equal(
            history.Id,
            result!.Id);

        Assert.Equal(
            "Test query",
            result.Query);

        Assert.Equal(
            "Test answer",
            result.Answer);

        Assert.True(result.IsGrounded);
        Assert.Equal(250, result.ResponseTimeMs);

        Assert.Single(result.Sources);

        Assert.Equal(
            chunkId,
            result.Sources[0].DocumentChunkId);
    }

    [Fact]
    public async Task GetByIdAsync_DifferentUser_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            QueryText = "Private query",
            AnswerText = "Private answer",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.QueryHistories.Add(history);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(
            otherUserId,
            history.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_MissingHistory_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        var result = await service.GetByIdAsync(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyUserId_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByIdAsync(
                Guid.Empty,
                Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_EmptyHistoryId_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByIdAsync(
                Guid.NewGuid(),
                Guid.Empty));
    }

    [Fact]
    public async Task GetHistoryAsync_InvalidPage_UsesFirstPage()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        dbContext.QueryHistories.Add(
            new QueryHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QueryText = "Test query",
                AnswerText = "Test answer",
                IsGrounded = true,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            userId,
            new QueryHistoryQueryParameters
            {
                Page = 0,
                PageSize = 10
            });

        Assert.Equal(1, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetHistoryAsync_InvalidPageSize_UsesDefaultPageSize()
    {
        await using var dbContext = CreateDbContext();

        var userId = Guid.NewGuid();

        dbContext.QueryHistories.Add(
            new QueryHistory
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                QueryText = "Test query",
                AnswerText = "Test answer",
                IsGrounded = true,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetHistoryAsync(
            userId,
            new QueryHistoryQueryParameters
            {
                Page = 1,
                PageSize = 0
            });

        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    private static QueryHistoryService CreateService(
        AppDbContext dbContext)
    {
        return new QueryHistoryService(dbContext);
    }

    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        return new AppDbContext(options);
    }
}