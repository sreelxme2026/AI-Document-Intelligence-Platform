namespace Application.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Guid documentId);

    ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken);
}