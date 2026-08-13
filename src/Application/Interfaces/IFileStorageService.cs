namespace Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Guid documentId,
        string fileName,
        Stream fileStream);

    Task DeleteAsync(string storagePath);
}