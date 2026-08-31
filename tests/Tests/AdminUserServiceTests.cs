using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tests;

public class AdminUserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        await using var dbContext = CreateDbContext();

        var user1 = CreateUser(
            "first@example.com",
            UserRole.DocumentUser);

        var user2 = CreateUser(
            "second@example.com",
            UserRole.DocumentUser);

        dbContext.Users.AddRange(user1, user2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalPages);

        Assert.Contains(
            result.Items,
            user => user.Email == "first@example.com");

        Assert.Contains(
            result.Items,
            user => user.Email == "second@example.com");
    }

    [Fact]
    public async Task GetUsersAsync_SearchFiltersByEmail()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.AddRange(
            CreateUser(
                "john@example.com",
                UserRole.DocumentUser),
            CreateUser(
                "jane@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                Search = "john"
            });

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(
            "john@example.com",
            result.Items[0].Email);
    }

    [Fact]
    public async Task GetUsersAsync_SearchFiltersByUserName()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "john@example.com",
            UserRole.DocumentUser);

        user.UserName = "johnny";

        dbContext.Users.Add(user);

        dbContext.Users.Add(
            CreateUser(
                "jane@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                Search = "johnny"
            });

        Assert.Single(result.Items);
        Assert.Equal(
            "john@example.com",
            result.Items[0].Email);
    }

    [Fact]
    public async Task GetUsersAsync_SearchMatchesStoredEmail()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(
            CreateUser(
                "john@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                Search = "john"
            });

        Assert.Single(result.Items);
        Assert.Equal(
            "john@example.com",
            result.Items[0].Email);
    }

    [Fact]
    public async Task GetUsersAsync_AppliesPagination()
    {
        await using var dbContext = CreateDbContext();

        for (var i = 1; i <= 5; i++)
        {
            dbContext.Users.Add(
                CreateUser(
                    $"user{i}@example.com",
                    UserRole.DocumentUser));
        }

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
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
    public async Task GetUsersAsync_InvalidPage_UsesFirstPage()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(
            CreateUser(
                "user@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                Page = 0
            });

        Assert.Equal(1, result.Page);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUsersAsync_InvalidPageSize_UsesDefaultPageSize()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(
            CreateUser(
                "user@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                PageSize = 0
            });

        Assert.Equal(10, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUsersAsync_PageSizeAboveMaximum_UsesMaximum()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(
            CreateUser(
                "user@example.com",
                UserRole.DocumentUser));

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters
            {
                PageSize = 1000
            });

        Assert.Equal(100, result.PageSize);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetUsersAsync_EmptyDatabase_ReturnsEmptyResult()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsUserInformation()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "admin@example.com",
            UserRole.Admin);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.GetUsersAsync(
            new AdminUserQueryParameters());

        var response = Assert.Single(result.Items);

        Assert.Equal(user.Id, response.Id);
        Assert.Equal(
            "admin@example.com",
            response.Email);
        Assert.Equal(
            user.UserName,
            response.UserName);
        Assert.Equal(
            UserRole.Admin.ToString(),
            response.Role);
        Assert.Equal(
            user.CreatedAt,
            response.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_MissingUser_ReturnsFalse()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(
            Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_EmptyUserId_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteAsync(Guid.Empty));
    }

    [Fact]
    public async Task DeleteAsync_DeletesUser()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "delete@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(user.Id);

        Assert.True(result);

        Assert.Null(
            await dbContext.Users
                .SingleOrDefaultAsync(
                    stored => stored.Id == user.Id));
    }

    [Fact]
    public async Task DeleteAsync_DeletesUserDocuments()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "documents@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(user);

        var document1 = CreateDocument(
            user.Id,
            "document1.pdf");

        var document2 = CreateDocument(
            user.Id,
            "document2.pdf");

        dbContext.Documents.AddRange(
            document1,
            document2);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(user.Id);

        Assert.True(result);

        Assert.Empty(
            await dbContext.Documents
                .Where(document =>
                    document.UploadedByUserId == user.Id)
                .ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_DeletesDocumentChunksAndEmbeddings()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "chunks@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(user);

        var document = CreateDocument(
            user.Id,
            "document.pdf");

        dbContext.Documents.Add(document);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Content = "Test chunk",
            TokenCount = 10
        };

        dbContext.DocumentChunks.Add(chunk);

        var embedding = new Embedding
        {
            Id = Guid.NewGuid(),
            DocumentChunkId = chunk.Id,
            VectorJson = "[0.1,0.2,0.3]",
            Model = "test-model"
        };

        dbContext.Embeddings.Add(embedding);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(user.Id);

        Assert.True(result);

        Assert.Empty(
            await dbContext.DocumentChunks
                .Where(stored =>
                    stored.DocumentId == document.Id)
                .ToListAsync());

        Assert.Empty(
            await dbContext.Embeddings
                .Where(stored =>
                    stored.DocumentChunkId == chunk.Id)
                .ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_DeletesUserQueryHistory()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "history@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(user);

        var history1 = CreateQueryHistory(
            user.Id,
            "Question 1");

        var history2 = CreateQueryHistory(
            user.Id,
            "Question 2");

        dbContext.QueryHistories.AddRange(
            history1,
            history2);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(user.Id);

        Assert.True(result);

        Assert.Empty(
            await dbContext.QueryHistories
                .Where(history =>
                    history.UserId == user.Id)
                .ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_RemovesSourcesReferencingDeletedUsersDocuments()
    {
        await using var dbContext = CreateDbContext();

        var user = CreateUser(
            "documents@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(user);

        var document = CreateDocument(
            user.Id,
            "document.pdf");

        dbContext.Documents.Add(document);

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Content = "Test chunk",
            TokenCount = 10
        };

        dbContext.DocumentChunks.Add(chunk);

        var otherUser = CreateUser(
            "other@example.com",
            UserRole.DocumentUser);

        dbContext.Users.Add(otherUser);

        var history = CreateQueryHistory(
            otherUser.Id,
            "Other user's query");

        dbContext.QueryHistories.Add(history);

        var source = new QueryHistorySource
        {
            Id = Guid.NewGuid(),
            QueryHistoryId = history.Id,
            DocumentChunkId = chunk.Id,
            RelevanceScore = 0.95f
        };

        dbContext.QueryHistorySources.Add(source);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.DeleteAsync(user.Id);

        Assert.True(result);

        Assert.Empty(
            await dbContext.QueryHistorySources
                .Where(stored =>
                    stored.DocumentChunkId == chunk.Id)
                .ToListAsync());

        Assert.NotNull(
            await dbContext.QueryHistories
                .SingleOrDefaultAsync(stored =>
                    stored.Id == history.Id));
    }

    private static AdminUserService CreateService(
        AppDbContext dbContext)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddIdentityCore<User>();

        services.AddSingleton<
            IPasswordHasher<User>,
            PasswordHasher<User>>();

        services.AddScoped<
            IUserStore<User>,
            UserStore<User, IdentityRole<Guid>, AppDbContext, Guid>>();

        services.AddScoped<
            UserManager<User>>();

        services.AddScoped(_ =>
            new FakeFileStorageService());

        var provider = services
            .AddSingleton(dbContext)
            .BuildServiceProvider();

        var userManager = provider
            .GetRequiredService<UserManager<User>>();

        var fileStorageService =
            provider.GetRequiredService<FakeFileStorageService>();

        return new AdminUserService(
            dbContext,
            userManager,
            fileStorageService);
    }

    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"AdminUserServiceTests-{Guid.NewGuid()}")
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics
                            .InMemoryEventId
                            .TransactionIgnoredWarning))
                .Options;

        return new AppDbContext(options);
    }

    private static User CreateUser(
        string email,
        UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Document CreateDocument(
        Guid userId,
        string fileName)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            OriginalFileName = fileName,
            ContentType = "application/pdf",
            FileSizeBytes = 1000,
            StoragePath = $"uploads/{fileName}",
            UploadedByUserId = userId,
            Status = DocumentStatus.Ready,
            UploadedAt = DateTime.UtcNow
        };
    }

    private static QueryHistory CreateQueryHistory(
        Guid userId,
        string query)
    {
        return new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            QueryText = query,
            AnswerText = "Test answer",
            IsGrounded = true,
            ResponseTimeMs = 100,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeFileStorageService
        : IFileStorageService
    {
        public List<string> DeletedPaths { get; } = [];

        public Task<string> SaveAsync(
            Guid documentId,
            string fileName,
            Stream fileStream)
        {
            return Task.FromResult(
                $"uploads/{fileName}");
        }

        public Task DeleteAsync(string storagePath)
        {
            DeletedPaths.Add(storagePath);

            return Task.CompletedTask;
        }
    }
}