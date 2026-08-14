using Application.DTOs;

namespace Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponse> UploadAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string? title,
        string? description,
        string? tags);

    Task<DocumentListResponse> GetDocumentsAsync(
        Guid userId,
        DocumentQueryParameters parameters);

    Task<DocumentResponse?> GetByIdAsync(
        Guid userId,
        Guid documentId);

    Task<DocumentStatusResponse?> GetStatusAsync(
        Guid userId,
        Guid documentId);

    Task<bool> DeleteAsync(
        Guid userId,
        Guid documentId);
}