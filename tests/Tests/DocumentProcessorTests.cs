using Application.DTOs;
using Application.Entities;
using Application.Enums;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class DocumentProcessorTests
{
    [Fact]
    public async Task ProcessAsync_Success_SetsDocumentToReadyAndCreatesChunks()
    {
        var documentId = Guid.NewGuid();

        await using var dbContext =
            CreateDbContext(documentId);

        dbContext.Documents.Add(
            CreateDocument(documentId));

        await dbContext.SaveChangesAsync();

        var extractor = new FakeTextExtractor();
        var chunker = new FakeTextChunker();
        var embeddingService = new FakeEmbeddingService();

        var processor = new DocumentProcessor(
            dbContext,
            extractor,
            chunker,
            embeddingService,
            NullLogger<DocumentProcessor>.Instance);

        await processor.ProcessAsync(
            documentId,
            CancellationToken.None);

        var document = await dbContext.Documents
            .SingleAsync(document =>
                document.Id == documentId);

        Assert.Equal(
            DocumentStatus.Ready,
            document.Status);

        Assert.Null(document.StatusMessage);
        Assert.NotNull(document.ProcessedAt);

        var chunks = await dbContext.DocumentChunks
            .Where(chunk =>
                chunk.DocumentId == documentId)
            .ToListAsync();

        Assert.Single(chunks);

        Assert.Equal(
            "This is a test chunk.",
            chunks[0].Content);

        Assert.Equal(
            10,
            chunks[0].TokenCount);

        Assert.True(
            embeddingService.WasCalled);
    }

    [Fact]
    public async Task ProcessAsync_ExtractionFails_SetsDocumentToFailed()
    {
        var documentId = Guid.NewGuid();

        await using var dbContext =
            CreateDbContext(documentId);

        dbContext.Documents.Add(
            CreateDocument(documentId));

        await dbContext.SaveChangesAsync();

        var extractor =
            new FailingTextExtractor();

        var chunker = new FakeTextChunker();
        var embeddingService = new FakeEmbeddingService();

        var processor = new DocumentProcessor(
            dbContext,
            extractor,
            chunker,
            embeddingService,
            NullLogger<DocumentProcessor>.Instance);

        await processor.ProcessAsync(
            documentId,
            CancellationToken.None);

        var document = await dbContext.Documents
            .SingleAsync(document =>
                document.Id == documentId);

        Assert.Equal(
            DocumentStatus.Failed,
            document.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                document.StatusMessage));

        Assert.Null(
            document.ProcessedAt);

        Assert.False(
            embeddingService.WasCalled);
    }

    [Fact]
    public async Task ProcessAsync_ChunkingProducesNoChunks_SetsDocumentToFailed()
    {
        var documentId = Guid.NewGuid();

        await using var dbContext =
            CreateDbContext(documentId);

        dbContext.Documents.Add(
            CreateDocument(documentId));

        await dbContext.SaveChangesAsync();

        var extractor = new FakeTextExtractor();

        var chunker =
            new EmptyTextChunker();

        var embeddingService = new FakeEmbeddingService();

        var processor = new DocumentProcessor(
            dbContext,
            extractor,
            chunker,
            embeddingService,
            NullLogger<DocumentProcessor>.Instance);

        await processor.ProcessAsync(
            documentId,
            CancellationToken.None);

        var document = await dbContext.Documents
            .SingleAsync(document =>
                document.Id == documentId);

        Assert.Equal(
            DocumentStatus.Failed,
            document.Status);

        Assert.False(
            string.IsNullOrWhiteSpace(
                document.StatusMessage));

        Assert.False(
            embeddingService.WasCalled);
    }

    private static AppDbContext CreateDbContext(
        Guid documentId)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DocumentProcessorTests-{documentId}")
                .Options;

        return new AppDbContext(options);
    }

    private static Document CreateDocument(
        Guid documentId)
    {
        return new Document
        {
            Id = documentId,
            FileName = "test.txt",
            OriginalFileName = "test.txt",
            ContentType = "text/plain",
            FileSizeBytes = 100,
            UploadedByUserId = Guid.NewGuid(),
            StoragePath = "test.txt",
            Status = DocumentStatus.Uploaded,
            UploadedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeTextExtractor : ITextExtractor
    {
        public Task<string> ExtractTextAsync(
            string filePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                "This is extracted test text.");
        }
    }

    private sealed class FailingTextExtractor : ITextExtractor
    {
        public Task<string> ExtractTextAsync(
            string filePath,
            string contentType,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Test extraction failure.");
        }
    }

    private sealed class FakeTextChunker : ITextChunker
    {
        public IReadOnlyList<TextChunkResult> Chunk(
            string text)
        {
            return
            [
                new TextChunkResult
                {
                    ChunkIndex = 0,
                    Content = "This is a test chunk.",
                    TokenCount = 10
                }
            ];
        }
    }

    private sealed class EmptyTextChunker : ITextChunker
    {
        public IReadOnlyList<TextChunkResult> Chunk(
            string text)
        {
            return [];
        }
    }

    private sealed class FakeEmbeddingService
        : IEmbeddingService
    {
        public bool WasCalled { get; private set; }

        public Task<float[]> GenerateEmbeddingAsync(
            string text)
        {
            WasCalled = true;

            return Task.FromResult(
                new[] { 0.1f, 0.2f, 0.3f });
        }

        public Task GenerateEmbeddingsForDocumentAsync(
            Guid documentId)
        {
            WasCalled = true;

            return Task.CompletedTask;
        }
    }
}