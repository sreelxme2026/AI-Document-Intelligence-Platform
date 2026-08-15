using System.Text.Json;
using Application.Configuration;
using Application.Entities;
using Application.Interfaces;
using Google.GenAI;
using Google.GenAI.Types;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly AppDbContext _dbContext;
    private readonly GeminiSettings _settings;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(
        AppDbContext dbContext,
        IOptions<GeminiSettings> settings,
        ILogger<EmbeddingService> logger)
    {
        _dbContext = dbContext;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text cannot be null or empty.",
                nameof(text));
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.EmbeddingModel))
        {
            throw new InvalidOperationException(
                "Gemini embedding model is not configured.");
        }

        var client = new Client(
            apiKey: _settings.ApiKey);

        var config = new EmbedContentConfig
        {
            OutputDimensionality = _settings.OutputDimension
        };

        var response = await client.Models.EmbedContentAsync(
            model: _settings.EmbeddingModel,
            contents: text,
            config: config);

        var values = response.Embeddings?
            .FirstOrDefault()?
            .Values;

        if (values is null || values.Count == 0)
        {
            throw new InvalidOperationException(
                "Gemini returned an empty embedding.");
        }

        return values
            .Select(value => (float)value)
            .ToArray();
    }

    public async Task GenerateEmbeddingsForDocumentAsync(
        Guid documentId)
    {
        var chunks = await _dbContext.DocumentChunks
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync();

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException(
                $"No document chunks were found for document {documentId}.");
        }

        var chunkIds = chunks
            .Select(chunk => chunk.Id)
            .ToList();

        var existingEmbeddings = await _dbContext.Embeddings
            .Where(embedding =>
                chunkIds.Contains(embedding.DocumentChunkId))
            .ToListAsync();

        var existingEmbeddingChunkIds = existingEmbeddings
            .Select(embedding => embedding.DocumentChunkId)
            .ToHashSet();

        var newEmbeddings = new List<Embedding>();

        foreach (var chunk in chunks)
        {
            if (existingEmbeddingChunkIds.Contains(chunk.Id))
            {
                continue;
            }

            _logger.LogInformation(
                "Generating Gemini embedding for document {DocumentId}, chunk {ChunkIndex}.",
                documentId,
                chunk.ChunkIndex);

            var vector = await GenerateEmbeddingAsync(
                chunk.Content);

            newEmbeddings.Add(
                new Embedding
                {
                    Id = Guid.NewGuid(),
                    DocumentChunkId = chunk.Id,
                    VectorJson = JsonSerializer.Serialize(vector),
                    Model = _settings.EmbeddingModel,
                    Dimension = vector.Length,
                    CreatedAt = DateTime.UtcNow
                });
        }

        if (newEmbeddings.Count > 0)
        {
            _dbContext.Embeddings.AddRange(
                newEmbeddings);

            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Generated {EmbeddingCount} Gemini embeddings for document {DocumentId}.",
            newEmbeddings.Count,
            documentId);
    }
}