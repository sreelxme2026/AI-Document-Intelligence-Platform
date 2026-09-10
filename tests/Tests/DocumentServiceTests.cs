using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public class DocumentServiceTests
{
    [Fact]
    public async Task GetDocumentsAsync_OwnerCanSeeOwnDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();

        var document = CreateDocument(
            "owner-document.pdf",
            owner.Id);

        context.Users.Add(owner);
        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            owner.Id,
            new DocumentQueryParameters());

        Assert.Single(result.Items);
        Assert.Equal(
            document.Id,
            result.Items[0].Id);
    }

    [Fact]
    public async Task GetDocumentsAsync_GrantedUserCanSeeDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var grantedUser = CreateUser();

        var document = CreateDocument(
            "shared-document.pdf",
            owner.Id);

        var access = CreateAccess(
            document.Id,
            grantedUser.Id);

        context.Users.AddRange(
            owner,
            grantedUser);

        context.Documents.Add(document);
        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            grantedUser.Id,
            new DocumentQueryParameters());

        Assert.Single(result.Items);

        Assert.Equal(
            document.Id,
            result.Items[0].Id);
    }

    [Fact]
    public async Task GetDocumentsAsync_UnrelatedUserCannotSeeDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var unrelatedUser = CreateUser();

        var document = CreateDocument(
            "private-document.pdf",
            owner.Id);

        context.Users.AddRange(
            owner,
            unrelatedUser);

        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            unrelatedUser.Id,
            new DocumentQueryParameters());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetDocumentsAsync_OnlyReturnsOwnedAndGrantedDocuments()
    {
        await using var context = CreateDbContext();

        var user = CreateUser();
        var otherUser = CreateUser();
        var thirdUser = CreateUser();

        var ownedDocument = CreateDocument(
            "owned.pdf",
            user.Id);

        var grantedDocument = CreateDocument(
            "granted.pdf",
            otherUser.Id);

        var privateDocument = CreateDocument(
            "private.pdf",
            thirdUser.Id);

        var access = CreateAccess(
            grantedDocument.Id,
            user.Id);

        context.Users.AddRange(
            user,
            otherUser,
            thirdUser);

        context.Documents.AddRange(
            ownedDocument,
            grantedDocument,
            privateDocument);

        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetDocumentsAsync(
            user.Id,
            new DocumentQueryParameters());

        Assert.Equal(2, result.TotalCount);

        Assert.Contains(
            result.Items,
            item => item.Id == ownedDocument.Id);

        Assert.Contains(
            result.Items,
            item => item.Id == grantedDocument.Id);

        Assert.DoesNotContain(
            result.Items,
            item => item.Id == privateDocument.Id);
    }

    [Fact]
    public async Task GetByIdAsync_OwnerCanGetDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();

        var document = CreateDocument(
            "document.pdf",
            owner.Id);

        context.Users.Add(owner);
        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(
            owner.Id,
            document.Id);

        Assert.NotNull(result);
        Assert.Equal(
            document.Id,
            result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_GrantedUserCanGetDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var grantedUser = CreateUser();

        var document = CreateDocument(
            "shared-document.pdf",
            owner.Id);

        var access = CreateAccess(
            document.Id,
            grantedUser.Id);

        context.Users.AddRange(
            owner,
            grantedUser);

        context.Documents.Add(document);
        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(
            grantedUser.Id,
            document.Id);

        Assert.NotNull(result);
        Assert.Equal(
            document.Id,
            result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnrelatedUserCannotGetDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var unrelatedUser = CreateUser();

        var document = CreateDocument(
            "private-document.pdf",
            owner.Id);

        context.Users.AddRange(
            owner,
            unrelatedUser);

        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetByIdAsync(
            unrelatedUser.Id,
            document.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStatusAsync_GrantedUserCanGetDocumentStatus()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var grantedUser = CreateUser();

        var document = CreateDocument(
            "shared-document.pdf",
            owner.Id);

        document.Status = DocumentStatus.Ready;
        document.StatusMessage = "Document processed successfully.";
        document.ProcessedAt = DateTime.UtcNow;

        var access = CreateAccess(
            document.Id,
            grantedUser.Id);

        context.Users.AddRange(
            owner,
            grantedUser);

        context.Documents.Add(document);
        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetStatusAsync(
            grantedUser.Id,
            document.Id);

        Assert.NotNull(result);

        Assert.Equal(
            document.Id,
            result.Id);

        Assert.Equal(
            "Ready",
            result.Status);

        Assert.Equal(
            "Document processed successfully.",
            result.StatusMessage);

        Assert.Equal(
            document.ProcessedAt,
            result.ProcessedAt);
    }

    [Fact]
    public async Task GetStatusAsync_UnrelatedUserCannotGetDocumentStatus()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var unrelatedUser = CreateUser();

        var document = CreateDocument(
            "private-document.pdf",
            owner.Id);

        context.Users.AddRange(
            owner,
            unrelatedUser);

        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetStatusAsync(
            unrelatedUser.Id,
            document.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_OwnerCanDeleteDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();

        var document = CreateDocument(
            "document.pdf",
            owner.Id);

        context.Users.Add(owner);
        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();

        var service = CreateService(
            context,
            storage: storage);

        var result = await service.DeleteAsync(
            owner.Id,
            document.Id);

        Assert.True(result);

        Assert.Null(
            await context.Documents
                .FirstOrDefaultAsync(
                    item => item.Id == document.Id));

        Assert.True(storage.DeleteCalled);

        Assert.Equal(
            document.StoragePath,
            storage.DeletedPath);
    }

    [Fact]
    public async Task DeleteAsync_GrantedUserCannotDeleteDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var grantedUser = CreateUser();

        var document = CreateDocument(
            "shared-document.pdf",
            owner.Id);

        var access = CreateAccess(
            document.Id,
            grantedUser.Id);

        context.Users.AddRange(
            owner,
            grantedUser);

        context.Documents.Add(document);
        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();

        var service = CreateService(
            context,
            storage: storage);

        var result = await service.DeleteAsync(
            grantedUser.Id,
            document.Id);

        Assert.False(result);

        Assert.NotNull(
            await context.Documents
                .FirstOrDefaultAsync(
                    item => item.Id == document.Id));

        Assert.False(storage.DeleteCalled);
    }

    [Fact]
    public async Task DeleteAsync_UnrelatedUserCannotDeleteDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var unrelatedUser = CreateUser();

        var document = CreateDocument(
            "private-document.pdf",
            owner.Id);

        context.Users.AddRange(
            owner,
            unrelatedUser);

        context.Documents.Add(document);

        await context.SaveChangesAsync();

        var storage = new FakeFileStorageService();

        var service = CreateService(
            context,
            storage: storage);

        var result = await service.DeleteAsync(
            unrelatedUser.Id,
            document.Id);

        Assert.False(result);

        Assert.NotNull(
            await context.Documents
                .FirstOrDefaultAsync(
                    item => item.Id == document.Id));

        Assert.False(storage.DeleteCalled);
    }

    [Fact]
    public async Task GetDocumentsAsync_AfterAccessIsRevoked_DoesNotReturnDocument()
    {
        await using var context = CreateDbContext();

        var owner = CreateUser();
        var grantedUser = CreateUser();

        var document = CreateDocument(
            "shared-document.pdf",
            owner.Id);

        var access = CreateAccess(
            document.Id,
            grantedUser.Id);

        context.Users.AddRange(
            owner,
            grantedUser);

        context.Documents.Add(document);
        context.DocumentAccesses.Add(access);

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var beforeRevoke =
            await service.GetDocumentsAsync(
                grantedUser.Id,
                new DocumentQueryParameters());

        Assert.Single(beforeRevoke.Items);

        context.DocumentAccesses.Remove(access);
        await context.SaveChangesAsync();

        var afterRevoke =
            await service.GetDocumentsAsync(
                grantedUser.Id,
                new DocumentQueryParameters());

        Assert.Empty(afterRevoke.Items);
    }

    private static DocumentService CreateService(
        AppDbContext context,
        IFileValidator? validator = null,
        IFileStorageService? storage = null,
        IBackgroundTaskQueue? queue = null)
    {
        return new DocumentService(
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
                    $"DocumentServiceTests-{Guid.NewGuid()}")
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(
                        Microsoft.EntityFrameworkCore.Diagnostics
                            .InMemoryEventId
                            .TransactionIgnoredWarning))
                .Options;

        return new AppDbContext(options);
    }

    private static Document CreateDocument(
        string fileName,
        Guid ownerId)
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
            UploadedByUserId = ownerId,
            Status = DocumentStatus.Ready,
            UploadedAt = DateTime.UtcNow
        };
    }

    private static DocumentAccess CreateAccess(
        Guid documentId,
        Guid userId)
    {
        return new DocumentAccess
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            GrantedAt = DateTime.UtcNow
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
            Role = UserRole.DocumentUser,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeFileValidator : IFileValidator
    {
        public void Validate(
            string fileName,
            string contentType,
            long fileSize)
        {
        }
    }

    private sealed class FakeFileStorageService
        : IFileStorageService
    {
        public bool DeleteCalled { get; private set; }

        public string? DeletedPath { get; private set; }

        public Task<string> SaveAsync(
            Guid documentId,
            string fileName,
            Stream fileStream)
        {
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

        public Task<Stream?> OpenReadAsync(
            string storagePath)
        {
            return Task.FromResult<Stream?>(
                null);
        }
    }

    private sealed class FakeBackgroundTaskQueue
        : IBackgroundTaskQueue
    {
        public ValueTask QueueAsync(
            Guid documentId)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<Guid> DequeueAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}