using Application.Interfaces;

namespace Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadRoot;

    public FileStorageService()
    {
        _uploadRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "App_Data",
            "uploads");
    }

    public async Task<string> SaveAsync(
        Guid documentId,
        string fileName,
        Stream fileStream)
    {
        var documentDirectory = Path.Combine(
            _uploadRoot,
            documentId.ToString());

        Directory.CreateDirectory(documentDirectory);

        var safeFileName = Path.GetFileName(fileName);

        var filePath = Path.Combine(
            documentDirectory,
            safeFileName);

        await using var outputStream =
            new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

        await fileStream.CopyToAsync(outputStream);

        return filePath;
    }

    public Task DeleteAsync(string storagePath)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }

        return Task.CompletedTask;
    }
}