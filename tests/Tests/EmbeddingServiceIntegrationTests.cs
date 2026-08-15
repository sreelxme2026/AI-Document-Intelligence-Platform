using Application.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tests;

public class EmbeddingServiceIntegrationTests
{
    private static bool IntegrationTestsEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithRealGemini_Returns1536DimensionEmbedding()
    {
        if (!IntegrationTestsEnabled())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable(
            "GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_API_KEY must be configured when integration tests are enabled.");
        }

        await using var dbContext = CreateDbContext();

        var service = new EmbeddingService(
            dbContext,
            Options.Create(
                new GeminiSettings
                {
                    ApiKey = apiKey,
                    EmbeddingModel = "gemini-embedding-2",
                    OutputDimension = 1536
                }),
            NullLogger<EmbeddingService>.Instance);

        var vector = await service.GenerateEmbeddingAsync(
            "This is a controlled Gemini embedding integration test.");

        Assert.NotNull(vector);
        Assert.NotEmpty(vector);
        Assert.Equal(1536, vector.Length);

        Assert.All(
            vector,
            value => Assert.False(float.IsNaN(value)));

        Assert.All(
            vector,
            value => Assert.False(float.IsInfinity(value)));
    }

    [Fact]
    public async Task GenerateEmbeddingsForDocumentAsync_WithRealGemini_PersistsEmbeddings()
    {
        if (!IntegrationTestsEnabled())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable(
            "GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_API_KEY must be configured when integration tests are enabled.");
        }

        var documentId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();

        dbContext.DocumentChunks.AddRange(
            new Application.Entities.DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = 0,
                Content = "This is the first document chunk.",
                TokenCount = 7,
                CreatedAt = DateTime.UtcNow
            },
            new Application.Entities.DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = 1,
                Content = "This is the second document chunk.",
                TokenCount = 7,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = new EmbeddingService(
            dbContext,
            Options.Create(
                new GeminiSettings
                {
                    ApiKey = apiKey,
                    EmbeddingModel = "gemini-embedding-2",
                    OutputDimension = 1536
                }),
            NullLogger<EmbeddingService>.Instance);

        await service.GenerateEmbeddingsForDocumentAsync(
            documentId);

        var embeddings = await dbContext.Embeddings
            .OrderBy(embedding => embedding.DocumentChunkId)
            .ToListAsync();

        Assert.Equal(2, embeddings.Count);

        Assert.All(
            embeddings,
            embedding =>
            {
                Assert.Equal(
                    "gemini-embedding-2",
                    embedding.Model);

                Assert.Equal(
                    1536,
                    embedding.Dimension);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        embedding.VectorJson));

                Assert.True(
                    embedding.CreatedAt != default);
            });
    }

    [Fact]
    public async Task GenerateEmbeddingsForDocumentAsync_CalledTwice_DoesNotCreateDuplicates()
    {
        if (!IntegrationTestsEnabled())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable(
            "GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_API_KEY must be configured when integration tests are enabled.");
        }

        var documentId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();

        var chunkId = Guid.NewGuid();

        dbContext.DocumentChunks.Add(
            new Application.Entities.DocumentChunk
            {
                Id = chunkId,
                DocumentId = documentId,
                ChunkIndex = 0,
                Content = "This is a test document chunk.",
                TokenCount = 7,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var service = new EmbeddingService(
            dbContext,
            Options.Create(
                new GeminiSettings
                {
                    ApiKey = apiKey,
                    EmbeddingModel = "gemini-embedding-2",
                    OutputDimension = 1536
                }),
            NullLogger<EmbeddingService>.Instance);

        await service.GenerateEmbeddingsForDocumentAsync(
            documentId);

        var firstCount = await dbContext.Embeddings
            .CountAsync();

        await service.GenerateEmbeddingsForDocumentAsync(
            documentId);

        var secondCount = await dbContext.Embeddings
            .CountAsync();

        Assert.Equal(1, firstCount);
        Assert.Equal(1, secondCount);

        var embedding = await dbContext.Embeddings
            .SingleAsync();

        Assert.Equal(
            chunkId,
            embedding.DocumentChunkId);

        Assert.Equal(
            1536,
            embedding.Dimension);
    }

    [Fact]
    public async Task DocumentProcessor_WithRealGemini_CompletesDocumentProcessing()
    {
        if (!IntegrationTestsEnabled())
        {
            return;
        }

        var apiKey = Environment.GetEnvironmentVariable(
            "GEMINI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "GEMINI_API_KEY must be configured when integration tests are enabled.");
        }

        var documentId = Guid.NewGuid();

        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"ai-document-test-{Guid.NewGuid()}.txt");

        try
        {
            await File.WriteAllTextAsync(
                tempFilePath,
                """
            This is an end-to-end document processing test.
            The document processor should extract this text,
            divide it into chunks, generate a Gemini embedding,
            save the embedding, and mark the document as ready.
            """);

            await using var dbContext = CreateDbContext();

            dbContext.Documents.Add(
                new Application.Entities.Document
                {
                    Id = documentId,
                    FileName = Path.GetFileName(tempFilePath),
                    OriginalFileName = "integration-test.txt",
                    ContentType = "text/plain",
                    FileSizeBytes = new FileInfo(tempFilePath).Length,
                    UploadedByUserId = Guid.NewGuid(),
                    StoragePath = tempFilePath,
                    Status = Application.Enums.DocumentStatus.Uploaded,
                    UploadedAt = DateTime.UtcNow
                });

            await dbContext.SaveChangesAsync();

            var settings = new GeminiSettings
            {
                ApiKey = apiKey,
                EmbeddingModel = "gemini-embedding-2",
                OutputDimension = 1536
            };

            var embeddingService = new EmbeddingService(
                dbContext,
                Options.Create(settings),
                NullLogger<EmbeddingService>.Instance);

            var processor = new Infrastructure.Services.DocumentProcessor(
                dbContext,
                new Infrastructure.Services.TextExtractor(),
                new Infrastructure.Services.TextChunker(),
                embeddingService,
                NullLogger<Infrastructure.Services.DocumentProcessor>.Instance);

            await processor.ProcessAsync(
                documentId,
                CancellationToken.None);

            var document = await dbContext.Documents
                .SingleAsync(document => document.Id == documentId);

            Assert.Equal(
                Application.Enums.DocumentStatus.Ready,
                document.Status);

            Assert.Null(document.StatusMessage);

            Assert.NotNull(document.ProcessedAt);

            var chunks = await dbContext.DocumentChunks
                .Where(chunk => chunk.DocumentId == documentId)
                .ToListAsync();

            Assert.NotEmpty(chunks);

            var embeddings = await dbContext.Embeddings
                .Where(embedding =>
                    chunks
                        .Select(chunk => chunk.Id)
                        .Contains(embedding.DocumentChunkId))
                .ToListAsync();

            Assert.Equal(
                chunks.Count,
                embeddings.Count);

            Assert.All(
                embeddings,
                embedding =>
                {
                    Assert.Equal(
                        "gemini-embedding-2",
                        embedding.Model);

                    Assert.Equal(
                        1536,
                        embedding.Dimension);

                    Assert.False(
                        string.IsNullOrWhiteSpace(
                            embedding.VectorJson));

                    var vector =
                        System.Text.Json.JsonSerializer
                            .Deserialize<float[]>(
                                embedding.VectorJson);

                    Assert.NotNull(vector);

                    Assert.Equal(
                        1536,
                        vector!.Length);
                });
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }
    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"EmbeddingIntegrationTests-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }
}