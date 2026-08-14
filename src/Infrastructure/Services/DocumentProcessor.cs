using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DocumentProcessor : IDocumentProcessor
{
    private readonly AppDbContext _dbContext;
    private readonly ITextExtractor _textExtractor;
    private readonly ITextChunker _textChunker;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(
        AppDbContext dbContext,
        ITextExtractor textExtractor,
        ITextChunker textChunker,
        IEmbeddingService embeddingService,
        ILogger<DocumentProcessor> logger)
    {
        _dbContext = dbContext;
        _textExtractor = textExtractor;
        _textChunker = textChunker;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task ProcessAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(
                document => document.Id == documentId,
                cancellationToken);

        if (document is null)
        {
            _logger.LogWarning(
                "Document {DocumentId} was not found for processing.",
                documentId);

            return;
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            document.StatusMessage = null;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Started processing document {DocumentId}.",
                documentId);

            var extractedText =
                await _textExtractor.ExtractTextAsync(
                    document.StoragePath,
                    document.ContentType,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new InvalidOperationException(
                    "No text could be extracted from the document.");
            }

            var chunks = _textChunker.Chunk(extractedText);

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "No chunks could be created from the extracted text.");
            }

            var documentChunks = chunks
                .Select(chunk => new Application.Entities.DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ChunkIndex = chunk.ChunkIndex,
                    Content = chunk.Content,
                    PageNumber = null,
                    TokenCount = chunk.TokenCount,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            _dbContext.DocumentChunks.AddRange(
                documentChunks);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Created {ChunkCount} chunks for document {DocumentId}.",
                documentChunks.Count,
                documentId);

            await _embeddingService
                .GenerateEmbeddingsForDocumentAsync(
                    documentId);

            document.Status = DocumentStatus.Ready;
            document.StatusMessage = null;
            document.ProcessedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Completed processing document {DocumentId}.",
                documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process document {DocumentId}.",
                documentId);

            document.Status = DocumentStatus.Failed;
            document.StatusMessage = ex.Message;
            document.ProcessedAt = null;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }
}