using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AdminDocumentService : IAdminDocumentService
{
    private readonly AppDbContext _dbContext;
    private readonly IFileValidator _fileValidator;
    private readonly IFileStorageService _fileStorageService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;

    public AdminDocumentService(
        AppDbContext dbContext,
        IFileValidator fileValidator,
        IFileStorageService fileStorageService,
        IBackgroundTaskQueue backgroundTaskQueue)
    {
        _dbContext = dbContext;
        _fileValidator = fileValidator;
        _fileStorageService = fileStorageService;
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(
        AdminDocumentQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var page = parameters.Page < 1
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize < 1
            ? 10
            : Math.Min(parameters.PageSize, 100);

        var query = _dbContext.Documents
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search
                .Trim()
                .ToLower();

            query = query.Where(document =>
                document.FileName.ToLower().Contains(search) ||
                document.OriginalFileName.ToLower().Contains(search) ||
                (document.Title != null &&
                 document.Title.ToLower().Contains(search)) ||
                (document.Description != null &&
                 document.Description.ToLower().Contains(search)) ||
                (document.Tags != null &&
                 document.Tags.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<DocumentStatus>(
                parameters.Status,
                true,
                out var status))
        {
            query = query.Where(document =>
                document.Status == status);
        }

        if (parameters.UploaderId.HasValue)
        {
            query = query.Where(document =>
                document.UploadedByUserId ==
                parameters.UploaderId.Value);
        }

        var totalCount = await query.CountAsync();

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)pageSize);

        var documents = await query
            .OrderByDescending(document =>
                document.UploadedAt)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new DocumentListResponse
        {
            Items = documents
                .Select(MapToResponse)
                .ToList(),

            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<DocumentResponse?> GetByIdAsync(
        Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document ID cannot be empty.",
                nameof(documentId));
        }

        var document = await _dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(document =>
                document.Id == documentId);

        return document is null
            ? null
            : MapToResponse(document);
    }

    public async Task<DocumentResponse> UploadAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string? title,
        string? description,
        string? tags)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(fileStream);

        _fileValidator.Validate(
            fileName,
            contentType,
            fileSize);

        var userExists = await _dbContext.Users
            .AnyAsync(user => user.Id == userId);

        if (!userExists)
        {
            throw new InvalidOperationException(
                "The specified user does not exist.");
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(fileName),
            OriginalFileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            UploadedByUserId = userId,
            Status = DocumentStatus.Uploaded,
            Title = title,
            Description = description,
            Tags = tags,
            UploadedAt = DateTime.UtcNow
        };

        document.StoragePath =
            await _fileStorageService.SaveAsync(
                document.Id,
                document.FileName,
                fileStream);

        try
        {
            _dbContext.Documents.Add(document);

            await _dbContext.SaveChangesAsync();

            await _backgroundTaskQueue.QueueAsync(
                document.Id);
        }
        catch
        {
            await _fileStorageService.DeleteAsync(
                document.StoragePath);

            throw;
        }

        return MapToResponse(document);
    }

    public async Task<bool> DeleteAsync(
        Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Document ID cannot be empty.",
                nameof(documentId));
        }

        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(document =>
                document.Id == documentId);

        if (document is null)
        {
            return false;
        }

        var documentChunkIds = await _dbContext.DocumentChunks
            .Where(chunk =>
                chunk.DocumentId == documentId)
            .Select(chunk => chunk.Id)
            .ToListAsync();

        if (documentChunkIds.Count > 0)
        {
            var queryHistorySources =
                await _dbContext.QueryHistorySources
                    .Where(source =>
                        documentChunkIds.Contains(
                            source.DocumentChunkId))
                    .ToListAsync();

            _dbContext.QueryHistorySources.RemoveRange(
                queryHistorySources);
        }

        _dbContext.Documents.Remove(document);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        try
        {
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await _fileStorageService.DeleteAsync(
            document.StoragePath);

        return true;
    }

    private static DocumentResponse MapToResponse(
        Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByUserId = document.UploadedByUserId,
            Status = document.Status.ToString(),
            StatusMessage = document.StatusMessage,
            UploadedAt = document.UploadedAt,
            ProcessedAt = document.ProcessedAt,
            PageCount = document.PageCount,
            Title = document.Title,
            Description = document.Description,
            Tags = document.Tags
        };
    }
}