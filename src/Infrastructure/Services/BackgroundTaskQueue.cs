using System.Threading.Channels;
using Application.Interfaces;

namespace Infrastructure.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Guid> _queue;

    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<Guid>();
    }

    public async ValueTask QueueAsync(Guid documentId)
    {
        await _queue.Writer.WriteAsync(documentId);
    }

    public async ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}