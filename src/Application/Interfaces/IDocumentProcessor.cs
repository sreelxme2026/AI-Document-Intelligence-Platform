namespace Application.Interfaces;

public interface IDocumentProcessor
{
    Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}