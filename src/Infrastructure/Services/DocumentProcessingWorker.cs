using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DocumentProcessingWorker : IHostedService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentProcessingWorker> _logger;

    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public DocumentProcessingWorker(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentProcessingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        _executingTask = ProcessQueueAsync(
            _stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stoppingCts is null ||
            _executingTask is null)
        {
            return;
        }

        _stoppingCts.Cancel();

        await Task.WhenAny(
            _executingTask,
            Task.Delay(Timeout.Infinite, cancellationToken));
    }

    private async Task ProcessQueueAsync(
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var documentId =
                    await _queue.DequeueAsync(
                        cancellationToken);

                using var scope =
                    _scopeFactory.CreateScope();

                var processor =
                    scope.ServiceProvider
                        .GetRequiredService<IDocumentProcessor>();

                await processor.ProcessAsync(
                    documentId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing a queued document.");
            }
        }
    }
}