using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _dbContext;
    private readonly IFileValidator _fileValidator;
    private readonly IFileStorageService _fileStorageService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;

    public DocumentService(
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
        _fileValidator.Validate(
            fileName,
            contentType,
            fileSize);

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

        _dbContext.Documents.Add(document);

        await _dbContext.SaveChangesAsync();

        await _backgroundTaskQueue.QueueAsync(document.Id);

        return MapToResponse(document);
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(
        Guid userId,
        DocumentQueryParameters parameters)
    {
        var query = _dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                document.UploadedByUserId == userId);

        if (!string.IsNullOrWhiteSpace(parameters.Status) &&
            Enum.TryParse<DocumentStatus>(
                parameters.Status,
                true,
                out var status))
        {
            query = query.Where(document =>
                document.Status == status);
        }

        var totalCount = await query.CountAsync();

        var page = parameters.Page < 1
            ? 1
            : parameters.Page;

        var pageSize = parameters.PageSize < 1
            ? 10
            : Math.Min(parameters.PageSize, 100);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        var documents = await query
            .OrderByDescending(document =>
                document.UploadedAt)
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
        Guid userId,
        Guid documentId)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(document =>
                document.Id == documentId &&
                document.UploadedByUserId == userId);

        return document is null
            ? null
            : MapToResponse(document);
    }

    public async Task<DocumentStatusResponse?> GetStatusAsync(
        Guid userId,
        Guid documentId)
    {
        var document = await _dbContext.Documents
            .AsNoTracking()
            .Where(document =>
                document.Id == documentId &&
                document.UploadedByUserId == userId)
            .Select(document => new DocumentStatusResponse
            {
                Id = document.Id,
                Status = document.Status.ToString(),
                StatusMessage = document.StatusMessage,
                ProcessedAt = document.ProcessedAt
            })
            .FirstOrDefaultAsync();

        return document;
    }

    public async Task<bool> DeleteAsync(
        Guid userId,
        Guid documentId)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(document =>
                document.Id == documentId &&
                document.UploadedByUserId == userId);

        if (document is null)
        {
            return false;
        }

        await _fileStorageService.DeleteAsync(
            document.StoragePath);

        _dbContext.Documents.Remove(document);

        await _dbContext.SaveChangesAsync();

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