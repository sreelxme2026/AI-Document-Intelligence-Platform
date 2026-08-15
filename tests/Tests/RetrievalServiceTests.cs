using System.Text.Json;
using Application.DTOs;
using Application.Entities;
using Application.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class RetrievalServiceTests
{
    [Fact]
    public async Task RetrieveAsync_EmptyQuery_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RetrieveAsync(
                new RetrievalRequest
                {
                    Query = "",
                    TopK = 5
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task RetrieveAsync_InvalidTopK_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.RetrieveAsync(
                new RetrievalRequest
                {
                    Query = "test query",
                    TopK = 0
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task RetrieveAsync_NoStoredEmbeddings_ReturnsEmptyResult()
    {
        await using var dbContext = CreateDbContext();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.Empty(result.Sources);
        Assert.True(
            embeddingService.WasCalled);
    }

    [Fact]
    public async Task RetrieveAsync_RanksSourcesBySimilarity()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var highlySimilarChunk =
            CreateChunk(
                documentId,
                0,
                "Highly relevant content.");

        var lessSimilarChunk =
            CreateChunk(
                documentId,
                1,
                "Less relevant content.");

        dbContext.DocumentChunks.AddRange(
            highlySimilarChunk,
            lessSimilarChunk);

        dbContext.Embeddings.AddRange(
            CreateEmbedding(
                highlySimilarChunk.Id,
                [1.0f, 0.0f]),
            CreateEmbedding(
                lessSimilarChunk.Id,
                [0.0f, 1.0f]));

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.Equal(
            2,
            result.Sources.Count);

        Assert.Equal(
            highlySimilarChunk.Id,
            result.Sources[0].DocumentChunkId);

        Assert.Equal(
            1.0,
            result.Sources[0].SimilarityScore,
            precision: 5);

        Assert.Equal(
            0.0,
            result.Sources[1].SimilarityScore,
            precision: 5);
    }

    [Fact]
    public async Task RetrieveAsync_TopKLimitsResults()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var chunks = Enumerable
            .Range(0, 5)
            .Select(index =>
                CreateChunk(
                    documentId,
                    index,
                    $"Content {index}"))
            .ToList();

        dbContext.DocumentChunks.AddRange(chunks);

        foreach (var chunk in chunks)
        {
            dbContext.Embeddings.Add(
                CreateEmbedding(
                    chunk.Id,
                    [1.0f, 0.0f]));
        }

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 2
            },
            CancellationToken.None);

        Assert.Equal(
            2,
            result.Sources.Count);
    }

    [Fact]
    public async Task RetrieveAsync_ReturnsCorrectChunkMetadata()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var chunk = CreateChunk(
            documentId,
            7,
            "Retrieved document content.");

        chunk.PageNumber = 12;

        dbContext.DocumentChunks.Add(chunk);

        dbContext.Embeddings.Add(
            CreateEmbedding(
                chunk.Id,
                [1.0f, 0.0f]));

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 1
            },
            CancellationToken.None);

        var source = Assert.Single(
            result.Sources);

        Assert.Equal(
            chunk.Id,
            source.DocumentChunkId);

        Assert.Equal(
            documentId,
            source.DocumentId);

        Assert.Equal(
            7,
            source.ChunkIndex);

        Assert.Equal(
            "Retrieved document content.",
            source.Content);

        Assert.Equal(
            12,
            source.PageNumber);
    }

    [Fact]
    public async Task RetrieveAsync_MismatchedDimensions_SkipsEmbedding()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var chunk = CreateChunk(
            documentId,
            0,
            "Mismatched vector content.");

        dbContext.DocumentChunks.Add(chunk);

        dbContext.Embeddings.Add(
            CreateEmbedding(
                chunk.Id,
                [1.0f, 0.0f, 0.0f]));

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.Empty(result.Sources);
    }

    [Fact]
    public async Task RetrieveAsync_InvalidVectorJson_SkipsEmbedding()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var chunk = CreateChunk(
            documentId,
            0,
            "Invalid vector content.");

        dbContext.DocumentChunks.Add(chunk);

        dbContext.Embeddings.Add(
            new Embedding
            {
                Id = Guid.NewGuid(),
                DocumentChunkId = chunk.Id,
                VectorJson = "not-valid-json",
                Model = "test-model",
                Dimension = 2,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.Empty(result.Sources);
    }

    [Fact]
    public async Task RetrieveAsync_ZeroVector_ReturnsZeroSimilarity()
    {
        await using var dbContext = CreateDbContext();

        var documentId = Guid.NewGuid();

        var chunk = CreateChunk(
            documentId,
            0,
            "Zero vector content.");

        dbContext.DocumentChunks.Add(chunk);

        dbContext.Embeddings.Add(
            CreateEmbedding(
                chunk.Id,
                [0.0f, 0.0f]));

        await dbContext.SaveChangesAsync();

        var embeddingService =
            new FakeEmbeddingService(
                [1.0f, 0.0f]);

        var service = CreateService(
            dbContext,
            embeddingService);

        var result = await service.RetrieveAsync(
            new RetrievalRequest
            {
                Query = "test query",
                TopK = 5
            },
            CancellationToken.None);

        var source = Assert.Single(
            result.Sources);

        Assert.Equal(
            0.0,
            source.SimilarityScore,
            precision: 5);
    }

    private static RetrievalService CreateService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService)
    {
        return new RetrievalService(
            dbContext,
            embeddingService,
            NullLogger<RetrievalService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"RetrievalServiceTests-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    private static DocumentChunk CreateChunk(
        Guid documentId,
        int chunkIndex,
        string content)
    {
        return new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content,
            PageNumber = null,
            TokenCount = 10,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Embedding CreateEmbedding(
        Guid documentChunkId,
        float[] vector)
    {
        return new Embedding
        {
            Id = Guid.NewGuid(),
            DocumentChunkId = documentChunkId,
            VectorJson = JsonSerializer.Serialize(vector),
            Model = "test-model",
            Dimension = vector.Length,
            CreatedAt = DateTime.UtcNow
        };
    }

    private sealed class FakeEmbeddingService
        : IEmbeddingService
    {
        private readonly float[] _embedding;

        public bool WasCalled { get; private set; }

        public FakeEmbeddingService(
            float[] embedding)
        {
            _embedding = embedding;
        }

        public Task<float[]> GenerateEmbeddingAsync(
            string text)
        {
            WasCalled = true;

            return Task.FromResult(
                _embedding);
        }

        public Task GenerateEmbeddingsForDocumentAsync(
            Guid documentId)
        {
            return Task.CompletedTask;
        }
    }
}