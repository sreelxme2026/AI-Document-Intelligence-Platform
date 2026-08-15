using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class RetrievalService : IRetrievalService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<RetrievalService> _logger;

    public RetrievalService(
        AppDbContext dbContext,
        IEmbeddingService embeddingService,
        ILogger<RetrievalService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<RetrievalResult> RetrieveAsync(
        RetrievalRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException(
                "Query cannot be null or empty.",
                nameof(request));
        }

        if (request.TopK <= 0)
        {
            throw new ArgumentException(
                "TopK must be greater than zero.",
                nameof(request));
        }

        _logger.LogInformation(
            "Starting retrieval for query with TopK {TopK}.",
            request.TopK);

        var queryVector =
            await _embeddingService.GenerateEmbeddingAsync(
                request.Query);

        if (queryVector.Length == 0)
        {
            throw new InvalidOperationException(
                "Query embedding is empty.");
        }

        var storedEmbeddings = await _dbContext.Embeddings
            .AsNoTracking()
            .Join(
                _dbContext.DocumentChunks.AsNoTracking(),
                embedding => embedding.DocumentChunkId,
                chunk => chunk.Id,
                (embedding, chunk) => new
                {
                    Embedding = embedding,
                    Chunk = chunk
                })
            .ToListAsync(cancellationToken);

        if (storedEmbeddings.Count == 0)
        {
            _logger.LogInformation(
                "No stored embeddings were found for retrieval.");

            return new RetrievalResult();
        }

        var scoredSources = new List<RetrievalSource>();

        foreach (var item in storedEmbeddings)
        {
            float[] vector;

            try
            {
                vector = JsonSerializer.Deserialize<float[]>(
                    item.Embedding.VectorJson)
                    ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Skipping embedding {EmbeddingId} because its vector JSON is invalid.",
                    item.Embedding.Id);

                continue;
            }

            if (vector.Length == 0)
            {
                _logger.LogWarning(
                    "Skipping embedding {EmbeddingId} because its vector is empty.",
                    item.Embedding.Id);

                continue;
            }

            if (vector.Length != queryVector.Length)
            {
                _logger.LogWarning(
                    "Skipping embedding {EmbeddingId} because its dimension {EmbeddingDimension} does not match query dimension {QueryDimension}.",
                    item.Embedding.Id,
                    vector.Length,
                    queryVector.Length);

                continue;
            }

            var similarity =
                CalculateCosineSimilarity(
                    queryVector,
                    vector);

            scoredSources.Add(
                new RetrievalSource
                {
                    DocumentChunkId = item.Chunk.Id,
                    DocumentId = item.Chunk.DocumentId,
                    ChunkIndex = item.Chunk.ChunkIndex,
                    Content = item.Chunk.Content,
                    PageNumber = item.Chunk.PageNumber,
                    SimilarityScore = similarity
                });
        }

        var sources = scoredSources
            .OrderByDescending(source => source.SimilarityScore)
            .Take(request.TopK)
            .ToList();

        _logger.LogInformation(
            "Retrieval completed with {SourceCount} sources.",
            sources.Count);

        return new RetrievalResult
        {
            Sources = sources
        };
    }

    private static double CalculateCosineSimilarity(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        if (left.Count != right.Count)
        {
            throw new ArgumentException(
                "Vectors must have the same dimension.");
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Count; i++)
        {
            dotProduct += left[i] * right[i];

            leftMagnitude +=
                left[i] * left[i];

            rightMagnitude +=
                right[i] * right[i];
        }

        if (leftMagnitude == 0 ||
            rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct /
            (Math.Sqrt(leftMagnitude) *
             Math.Sqrt(rightMagnitude));
    }
}