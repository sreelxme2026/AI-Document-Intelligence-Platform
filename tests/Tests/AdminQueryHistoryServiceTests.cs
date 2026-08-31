using Application.DTOs;
using Application.Entities;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class AdminQueryHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_ReturnsAllHistory()
    {
        await using var context = CreateContext();

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        context.Users.AddRange(
            new User
            {
                Id = userId1,
                Email = "user1@example.com",
                UserName = "user1@example.com",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = userId2,
                Email = "user2@example.com",
                UserName = "user2@example.com",
                CreatedAt = DateTime.UtcNow
            });

        context.QueryHistories.AddRange(
            CreateHistory(
                userId1,
                "What is the leave policy?",
                DateTime.UtcNow.AddMinutes(-2)),
            CreateHistory(
                userId2,
                "What is the attendance policy?",
                DateTime.UtcNow.AddMinutes(-1)));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_SearchesQueryText()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        context.QueryHistories.AddRange(
            CreateHistory(
                userId,
                "What is the leave policy?",
                DateTime.UtcNow.AddMinutes(-2)),
            CreateHistory(
                userId,
                "What is the attendance policy?",
                DateTime.UtcNow.AddMinutes(-1)));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                Search = "leave"
            });

        var item = Assert.Single(result.Items);

        Assert.Equal(
            "What is the leave policy?",
            item.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_SearchIsCaseInsensitive()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        context.QueryHistories.Add(
            CreateHistory(
                userId,
                "What is the Leave Policy?",
                DateTime.UtcNow));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                Search = "leave"
            });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersByUser()
    {
        await using var context = CreateContext();

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        context.QueryHistories.AddRange(
            CreateHistory(
                userId1,
                "Query from user one",
                DateTime.UtcNow.AddMinutes(-2)),
            CreateHistory(
                userId2,
                "Query from user two",
                DateTime.UtcNow.AddMinutes(-1)));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                UserId = userId1
            });

        var item = Assert.Single(result.Items);

        Assert.Equal(userId1, item.UserId);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersFromDate()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        var oldDate = new DateTime(
            2026,
            8,
            10,
            12,
            0,
            0,
            DateTimeKind.Utc);

        var newDate = new DateTime(
            2026,
            8,
            20,
            12,
            0,
            0,
            DateTimeKind.Utc);

        context.QueryHistories.AddRange(
            CreateHistory(
                userId,
                "Old query",
                oldDate),
            CreateHistory(
                userId,
                "New query",
                newDate));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                FromDate = new DateTime(
                    2026,
                    8,
                    15,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)
            });

        var item = Assert.Single(result.Items);

        Assert.Equal("New query", item.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_FiltersToDate()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        var oldDate = new DateTime(
            2026,
            8,
            10,
            12,
            0,
            0,
            DateTimeKind.Utc);

        var newDate = new DateTime(
            2026,
            8,
            20,
            12,
            0,
            0,
            DateTimeKind.Utc);

        context.QueryHistories.AddRange(
            CreateHistory(
                userId,
                "Old query",
                oldDate),
            CreateHistory(
                userId,
                "New query",
                newDate));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                ToDate = new DateTime(
                    2026,
                    8,
                    15,
                    23,
                    59,
                    59,
                    DateTimeKind.Utc)
            });

        var item = Assert.Single(result.Items);

        Assert.Equal("Old query", item.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_AppliesCombinedFilters()
    {
        await using var context = CreateContext();

        var matchingUser = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        context.QueryHistories.AddRange(
            CreateHistory(
                matchingUser,
                "Leave policy",
                new DateTime(
                    2026,
                    8,
                    20,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc)),
            CreateHistory(
                matchingUser,
                "Attendance policy",
                new DateTime(
                    2026,
                    8,
                    20,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc)),
            CreateHistory(
                otherUser,
                "Leave policy",
                new DateTime(
                    2026,
                    8,
                    20,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc)));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                Search = "leave",
                UserId = matchingUser,
                FromDate = new DateTime(
                    2026,
                    8,
                    19,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                ToDate = new DateTime(
                    2026,
                    8,
                    21,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)
            });

        var item = Assert.Single(result.Items);

        Assert.Equal(
            matchingUser,
            item.UserId);

        Assert.Equal(
            "Leave policy",
            item.Query);
    }

    [Fact]
    public async Task GetHistoryAsync_PaginatesResults()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        for (var i = 1; i <= 5; i++)
        {
            context.QueryHistories.Add(
                CreateHistory(
                    userId,
                    $"Query {i}",
                    new DateTime(
                        2026,
                        8,
                        1,
                        i,
                        0,
                        0,
                        DateTimeKind.Utc)));
        }

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetHistoryAsync_OrdersNewestFirst()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        var older = new DateTime(
            2026,
            8,
            1,
            10,
            0,
            0,
            DateTimeKind.Utc);

        var newer = older.AddHours(1);

        context.QueryHistories.AddRange(
            CreateHistory(
                userId,
                "Older",
                older),
            CreateHistory(
                userId,
                "Newer",
                newer));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters());

        Assert.Equal(
            "Newer",
            result.Items[0].Query);

        Assert.Equal(
            "Older",
            result.Items[1].Query);
    }

    [Fact]
    public async Task GetHistoryAsync_LimitsPageSizeTo100()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();

        context.QueryHistories.Add(
            CreateHistory(
                userId,
                "Test query",
                DateTime.UtcNow));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                PageSize = 1000
            });

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task GetHistoryAsync_NormalizesInvalidPage()
    {
        await using var context = CreateContext();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                Page = 0
            });

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetHistoryAsync_NormalizesInvalidPageSize()
    {
        await using var context = CreateContext();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters
            {
                PageSize = 0
            });

        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsSources()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        context.QueryHistories.Add(
            new QueryHistory
            {
                Id = historyId,
                UserId = userId,
                QueryText = "What is the leave policy?",
                AnswerText = "The leave policy is...",
                IsGrounded = true,
                CreatedAt = DateTime.UtcNow,
                ResponseTimeMs = 250
            });

        context.QueryHistorySources.Add(
            new QueryHistorySource
            {
                Id = Guid.NewGuid(),
                QueryHistoryId = historyId,
                DocumentChunkId = chunkId,
                RelevanceScore = 0.95f
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetHistoryAsync(
            new AdminQueryHistoryQueryParameters());

        var item = Assert.Single(result.Items);
        var source = Assert.Single(item.Sources);

        Assert.Equal(chunkId, source.DocumentChunkId);
        Assert.Equal(0.95f, source.RelevanceScore);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsHistory()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();
        var history = CreateHistory(
            userId,
            "What is the leave policy?",
            DateTime.UtcNow);

        context.QueryHistories.Add(history);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(history.Id);

        Assert.NotNull(result);
        Assert.Equal(history.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(
            "What is the leave policy?",
            result.Query);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSources()
    {
        await using var context = CreateContext();

        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        context.QueryHistories.Add(
            new QueryHistory
            {
                Id = historyId,
                UserId = userId,
                QueryText = "Test query",
                AnswerText = "Test answer",
                IsGrounded = true,
                CreatedAt = DateTime.UtcNow
            });

        context.QueryHistorySources.Add(
            new QueryHistorySource
            {
                Id = Guid.NewGuid(),
                QueryHistoryId = historyId,
                DocumentChunkId = chunkId,
                RelevanceScore = 0.8f
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result =
            await service.GetByIdAsync(historyId);

        Assert.NotNull(result);

        var source = Assert.Single(result.Sources);

        Assert.Equal(
            chunkId,
            source.DocumentChunkId);

        Assert.Equal(
            0.8f,
            source.RelevanceScore);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenHistoryDoesNotExist()
    {
        await using var context = CreateContext();

        var service = CreateService(context);

        var result =
            await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_RejectsEmptyId()
    {
        await using var context = CreateContext();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task GetHistoryAsync_ThrowsForNullParameters()
    {
        await using var context = CreateContext();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetHistoryAsync(null!));
    }

    private static AdminQueryHistoryService CreateService(
        AppDbContext context)
    {
        return new AdminQueryHistoryService(context);
    }

    private static QueryHistory CreateHistory(
        Guid userId,
        string query,
        DateTime createdAt)
    {
        return new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = query,
            AnswerText = $"Answer for: {query}",
            IsGrounded = true,
            CreatedAt = createdAt,
            ResponseTimeMs = 100
        };
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}