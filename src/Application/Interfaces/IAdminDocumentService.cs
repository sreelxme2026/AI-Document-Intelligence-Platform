using Application.DTOs;

namespace Application.Interfaces;

public interface IAdminDocumentService
{
    Task<DocumentListResponse> GetDocumentsAsync(
        AdminDocumentQueryParameters parameters);

    Task<DocumentResponse?> GetByIdAsync(
        Guid documentId);

    Task<DocumentResponse> UploadAsync(
        Guid userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string? title,
        string? description,
        string? tags);

    Task<bool> DeleteAsync(
        Guid documentId);
}