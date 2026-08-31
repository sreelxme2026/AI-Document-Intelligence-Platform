using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Exceptions;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public class AdminDocumentServiceTests
{
    [Fact]
    public async Task GetDocumentsAsync_ReturnsAllDocuments()
    {
        await using var context = CreateDbContext();

        var document1 = CreateDocument("first.pdf");
        var document2 = CreateDocument("second.pdf");

        context.Documents.AddRange(document1, document2);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchesFileName()
    {
        await using var context = CreateDbContext();

        context.Documents.AddRange(
            CreateDocument("annual-report.pdf"),
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "ANNUAL"
            });

        Assert.Single(result.Items);
        Assert.Equal(
            "annual-report.pdf",
            result.Items[0].FileName);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchesOriginalFileName()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("stored.pdf");
        document.OriginalFileName = "AnnualPolicy.pdf";

        context.Documents.AddRange(
            document,
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "annualpolicy"
            });

        Assert.Single(result.Items);
        Assert.Equal(
            "AnnualPolicy.pdf",
            result.Items[0].OriginalFileName);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchesTitle()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");
        document.Title = "Company Leave Policy";

        context.Documents.AddRange(
            document,
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "leave"
            });

        Assert.Single(result.Items);
        Assert.Equal(
            "Company Leave Policy",
            result.Items[0].Title);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchesDescription()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");
        document.Description =
            "Contains employee leave information.";

        context.Documents.AddRange(
            document,
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "employee leave"
            });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchesTags()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");
        document.Tags = "hr,policy,leave";

        context.Documents.AddRange(
            document,
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "policy"
            });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDocumentsAsync_SearchIsCaseInsensitive()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("Annual-Report.pdf");
        document.Title = "Annual Financial Report";

        context.Documents.AddRange(
            document,
            CreateDocument("other.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "annual"
            });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDocumentsAsync_FiltersByStatus()
    {
        await using var context = CreateDbContext();

        var ready = CreateDocument("ready.pdf");
        ready.Status = DocumentStatus.Ready;

        var failed = CreateDocument("failed.pdf");
        failed.Status = DocumentStatus.Failed;

        context.Documents.AddRange(ready, failed);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Status = "Ready"
            });

        Assert.Single(result.Items);
        Assert.Equal("Ready", result.Items[0].Status);
    }

    [Fact]
    public async Task GetDocumentsAsync_StatusFilterIsCaseInsensitive()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("ready.pdf");
        document.Status = DocumentStatus.Ready;

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Status = "ready"
            });

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetDocumentsAsync_FiltersByUploader()
    {
        await using var context = CreateDbContext();

        var user1 = CreateUser();
        var user2 = CreateUser();

        var document1 = CreateDocument("user1.pdf");
        document1.UploadedByUserId = user1.Id;

        var document2 = CreateDocument("user2.pdf");
        document2.UploadedByUserId = user2.Id;

        context.Users.AddRange(user1, user2);
        context.Documents.AddRange(document1, document2);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                UploaderId = user1.Id
            });

        Assert.Single(result.Items);
        Assert.Equal(
            user1.Id,
            result.Items[0].UploadedByUserId);
    }

    [Fact]
    public async Task GetDocumentsAsync_CombinesSearchStatusAndUploaderFilters()
    {
        await using var context = CreateDbContext();

        var user1 = CreateUser();
        var user2 = CreateUser();

        var matching = CreateDocument("annual.pdf");
        matching.UploadedByUserId = user1.Id;
        matching.Status = DocumentStatus.Ready;
        matching.Title = "Annual Policy";

        var wrongStatus = CreateDocument("annual-wrong-status.pdf");
        wrongStatus.UploadedByUserId = user1.Id;
        wrongStatus.Status = DocumentStatus.Failed;
        wrongStatus.Title = "Annual Policy";

        var wrongUser = CreateDocument("annual-wrong-user.pdf");
        wrongUser.UploadedByUserId = user2.Id;
        wrongUser.Status = DocumentStatus.Ready;
        wrongUser.Title = "Annual Policy";

        context.Users.AddRange(user1, user2);
        context.Documents.AddRange(
            matching,
            wrongStatus,
            wrongUser);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Search = "annual",
                Status = "ready",
                UploaderId = user1.Id
            });

        Assert.Single(result.Items);
        Assert.Equal(
            matching.Id,
            result.Items[0].Id);
    }

    [Fact]
    public async Task GetDocumentsAsync_PaginatesResults()
    {
        await using var context = CreateDbContext();

        for (var i = 0; i < 5; i++)
        {
            context.Documents.Add(
                CreateDocument($"document-{i}.pdf"));
        }

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetDocumentsAsync_InvalidPageDefaultsToOne()
    {
        await using var context = CreateDbContext();

        context.Documents.Add(
            CreateDocument("document.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                Page = 0
            });

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetDocumentsAsync_InvalidPageSizeDefaultsToTen()
    {
        await using var context = CreateDbContext();

        context.Documents.Add(
            CreateDocument("document.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                PageSize = 0
            });

        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetDocumentsAsync_PageSizeIsLimitedTo100()
    {
        await using var context = CreateDbContext();

        context.Documents.Add(
            CreateDocument("document.pdf"));

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters
            {
                PageSize = 500
            });

        Assert.Equal(100, result.PageSize);
    }

    [Fact]
    public async Task GetDocumentsAsync_OrdersNewestFirst()
    {
        await using var context = CreateDbContext();

        var older = CreateDocument("older.pdf");
        older.UploadedAt =
            new DateTime(2026, 1, 1);

        var newer = CreateDocument("newer.pdf");
        newer.UploadedAt =
            new DateTime(2026, 2, 1);

        context.Documents.AddRange(older, newer);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            new AdminDocumentQueryParameters());

        Assert.Equal(
            newer.Id,
            result.Items[0].Id);

        Assert.Equal(
            older.Id,
            result.Items[1].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDocument()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");
        document.Title = "Test Document";

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result =
            await service.GetByIdAsync(document.Id);

        Assert.NotNull(result);
        Assert.Equal(document.Id, result.Id);
        Assert.Equal("document.pdf", result.FileName);
        Assert.Equal(
            "Test Document",
            result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_MissingDocumentReturnsNull()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        var result =
            await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyIdThrows()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByIdAsync(Guid.Empty));
    }

    [Fact]
    public async Task UploadAsync_CreatesDocumentAndQueuesProcessing()
    {
        await using var context = CreateDbContext();

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var validator = new FakeFileValidator();
        var storage = new FakeFileStorageService();
        var queue = new FakeBackgroundTaskQueue();

        var service = CreateService(
            context,
            validator,
            storage,
            queue);

        await using var stream =
            new MemoryStream(
                "test content"u8.ToArray());

        var result = await service.UploadAsync(
            user.Id,
            stream,
            "test.pdf",
            "application/pdf",
            stream.Length,
            "Test title",
            "Test description",
            "test,document");

        var savedDocument =
            await context.Documents.SingleAsync();

        Assert.Equal(
            savedDocument.Id,
            result.Id);

        Assert.Equal(
            "test.pdf",
            savedDocument.FileName);

        Assert.Equal(
            "test.pdf",
            savedDocument.OriginalFileName);

        Assert.Equal(
            "application/pdf",
            savedDocument.ContentType);

        Assert.Equal(
            stream.Length,
            savedDocument.FileSizeBytes);

        Assert.Equal(
            user.Id,
            savedDocument.UploadedByUserId);

        Assert.Equal(
            DocumentStatus.Uploaded,
            savedDocument.Status);

        Assert.Equal(
            "Test title",
            savedDocument.Title);

        Assert.Equal(
            "Test description",
            savedDocument.Description);

        Assert.Equal(
            "test,document",
            savedDocument.Tags);

        Assert.Contains(
            savedDocument.Id,
            queue.QueuedDocumentIds);

        Assert.True(storage.Saved);
    }

    [Fact]
    public async Task UploadAsync_CallsFileValidator()
    {
        await using var context = CreateDbContext();

        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var validator = new FakeFileValidator();
        var storage = new FakeFileStorageService();
        var queue = new FakeBackgroundTaskQueue();

        var service = CreateService(
            context,
            validator,
            storage,
            queue);

        await using var stream =
            new MemoryStream(
                "test content"u8.ToArray());

        await service.UploadAsync(
            user.Id,
            stream,
            "test.pdf",
            "application/pdf",
            stream.Length,
            null,
            null,
            null);

        Assert.True(validator.Called);
        Assert.Equal("test.pdf", validator.FileName);
        Assert.Equal(
            "application/pdf",
            validator.ContentType);
        Assert.Equal(
            stream.Length,
            validator.FileSize);
    }

    [Fact]
    public async Task UploadAsync_MissingUserThrows()
    {
        await using var context = CreateDbContext();

        var validator = new FakeFileValidator();
        var storage = new FakeFileStorageService();
        var queue = new FakeBackgroundTaskQueue();

        var service = CreateService(
            context,
            validator,
            storage,
            queue);

        await using var stream =
            new MemoryStream(
                "test content"u8.ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(
                Guid.NewGuid(),
                stream,
                "test.pdf",
                "application/pdf",
                stream.Length,
                null,
                null,
                null));

        Assert.False(storage.Saved);
        Assert.Empty(queue.QueuedDocumentIds);
        Assert.Empty(context.Documents);
    }

    [Fact]
    public async Task UploadAsync_EmptyUserIdThrows()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        await using var stream =
            new MemoryStream(
                "test content"u8.ToArray());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UploadAsync(
                Guid.Empty,
                stream,
                "test.pdf",
                "application/pdf",
                stream.Length,
                null,
                null,
                null));
    }

    [Fact]
    public async Task UploadAsync_NullStreamThrows()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UploadAsync(
                Guid.NewGuid(),
                null!,
                "test.pdf",
                "application/pdf",
                10,
                null,
                null,
                null));
    }

    [Fact]
    public async Task UploadAsync_WhenQueueFails_DeletesStoredFile()
    {
        await using var context = CreateDbContext();

        var user = CreateUser();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var validator = new FakeFileValidator();
        var storage = new FakeFileStorageService();

        var queue = new FakeBackgroundTaskQueue
        {
            ThrowOnQueue = true
        };

        var service = CreateService(
            context,
            validator,
            storage,
            queue);

        await using var stream =
            new MemoryStream(
                "test content"u8.ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadAsync(
                user.Id,
                stream,
                "test.pdf",
                "application/pdf",
                stream.Length,
                null,
                null,
                null));

        Assert.True(storage.Saved);
        Assert.True(storage.DeleteCalled);
    }

    [Fact]
    public async Task DeleteAsync_MissingDocumentReturnsFalse()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        var result =
            await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_EmptyIdThrows()
    {
        await using var context = CreateDbContext();

        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteAsync(Guid.Empty));
    }

    [Fact]
    public async Task DeleteAsync_DeletesDocument()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");

        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();

        var service = CreateService(
            context,
            storage: storage);

        var result =
            await service.DeleteAsync(document.Id);

        Assert.True(result);

        Assert.Null(
            await context.Documents
                .FirstOrDefaultAsync(
                    d => d.Id == document.Id));

        Assert.True(storage.DeleteCalled);

        Assert.Equal(
            document.StoragePath,
            storage.DeletedPath);
    }

    [Fact]
    public async Task DeleteAsync_DeletesQueryHistorySources()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");

        var chunk = new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Content = "Document content",
            CreatedAt = DateTime.UtcNow
        };

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = document.UploadedByUserId,
            QueryText = "What is this document?",
            AnswerText = "Test answer",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow
        };

        var source = new QueryHistorySource
        {
            Id = Guid.NewGuid(),
            QueryHistoryId = history.Id,
            DocumentChunkId = chunk.Id,
            RelevanceScore = 0.9f
        };

        context.Documents.Add(document);
        context.DocumentChunks.Add(chunk);
        context.QueryHistories.Add(history);
        context.QueryHistorySources.Add(source);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result =
            await service.DeleteAsync(document.Id);

        Assert.True(result);

        Assert.Empty(
            await context.QueryHistorySources
                .Where(s =>
                    s.DocumentChunkId == chunk.Id)
                .ToListAsync());

        Assert.Empty(
            await context.DocumentChunks
                .Where(c =>
                    c.DocumentId == document.Id)
                .ToListAsync());

        Assert.Null(
            await context.Documents
                .FirstOrDefaultAsync(
                    d => d.Id == document.Id));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotDeleteQueryHistory()
    {
        await using var context = CreateDbContext();

        var document = CreateDocument("document.pdf");

        var history = new QueryHistory
        {
            Id = Guid.NewGuid(),
            UserId = document.UploadedByUserId,
            QueryText = "Test query",
            AnswerText = "Test answer",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Documents.Add(document);
        context.QueryHistories.Add(history);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        await service.DeleteAsync(document.Id);

        Assert.NotNull(
            await context.QueryHistories
                .FirstOrDefaultAsync(
                    q => q.Id == history.Id));
    }

    private static AdminDocumentService CreateService(
        AppDbContext context,
        IFileValidator? validator = null,
        IFileStorageService? storage = null,
        IBackgroundTaskQueue? queue = null)
    {
        return new AdminDocumentService(
            context,
            validator ?? new FakeFileValidator(),
            storage ?? new FakeFileStorageService(),
            queue ?? new FakeBackgroundTaskQueue());
    }

    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"AdminDocumentServiceTests-{Guid.NewGuid()}")
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics
                            .InMemoryEventId
                            .TransactionIgnoredWarning))
                .Options;

        return new AppDbContext(options);
    }

    private static Document CreateDocument(
        string fileName)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            OriginalFileName = fileName,
            ContentType = "application/pdf",
            FileSizeBytes = 100,
            StoragePath =
                Path.Combine(
                    "App_Data",
                    "uploads",
                    Guid.NewGuid().ToString(),
                    fileName),
            UploadedByUserId = Guid.NewGuid(),
            Status = DocumentStatus.Ready,
            UploadedAt = DateTime.UtcNow
        };
    }

    private static User CreateUser()
    {
        var email =
            $"user-{Guid.NewGuid():N}@example.com";

        return new User
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            Role = Application.Enums.UserRole.DocumentUser,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeFileValidator : IFileValidator
    {
        public bool Called { get; private set; }

        public string? FileName { get; private set; }

        public string? ContentType { get; private set; }

        public long FileSize { get; private set; }

        public void Validate(
            string fileName,
            string contentType,
            long fileSize)
        {
            Called = true;
            FileName = fileName;
            ContentType = contentType;
            FileSize = fileSize;
        }
    }

    private sealed class FakeFileStorageService
        : IFileStorageService
    {
        public bool Saved { get; private set; }

        public bool DeleteCalled { get; private set; }

        public string? DeletedPath { get; private set; }

        public Task<string> SaveAsync(
            Guid documentId,
            string fileName,
            Stream fileStream)
        {
            Saved = true;

            return Task.FromResult(
                Path.Combine(
                    "App_Data",
                    "uploads",
                    documentId.ToString(),
                    fileName));
        }

        public Task DeleteAsync(string storagePath)
        {
            DeleteCalled = true;
            DeletedPath = storagePath;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeBackgroundTaskQueue
        : IBackgroundTaskQueue
    {
        public List<Guid> QueuedDocumentIds { get; } = [];

        public bool ThrowOnQueue { get; set; }

        public ValueTask QueueAsync(
            Guid documentId)
        {
            if (ThrowOnQueue)
            {
                throw new InvalidOperationException(
                    "Simulated queue failure.");
            }

            QueuedDocumentIds.Add(documentId);

            return ValueTask.CompletedTask;
        }

        public ValueTask<Guid> DequeueAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}