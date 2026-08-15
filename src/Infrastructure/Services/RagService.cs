using Application.Configuration;
using Application.DTOs;
using Application.Interfaces;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class RagService : IRagService
{
    private readonly IRetrievalService _retrievalService;
    private readonly GeminiSettings _settings;
    private readonly ILogger<RagService> _logger;

    public RagService(
        IRetrievalService retrievalService,
        IOptions<GeminiSettings> settings,
        ILogger<RagService> logger)
    {
        _retrievalService = retrievalService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RagResult> GenerateAnswerAsync(
        RagRequest request,
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

        if (request.TopK <= 0 || request.TopK > 20)
        {
            throw new ArgumentException(
                "TopK must be between 1 and 20.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.GenerationModel))
        {
            throw new InvalidOperationException(
                "Gemini generation model is not configured.");
        }

        _logger.LogInformation(
            "Starting RAG generation for query with TopK {TopK}.",
            request.TopK);

        var retrievalResult =
            await _retrievalService.RetrieveAsync(
                new RetrievalRequest
                {
                    Query = request.Query,
                    TopK = request.TopK
                },
                cancellationToken);

        if (retrievalResult.Sources.Count == 0)
        {
            _logger.LogInformation(
                "No relevant sources were found for the query.");

            return new RagResult
            {
                Answer =
                    "I could not find relevant information in the available documents.",
                Sources = []
            };
        }

        var prompt =
            BuildGroundedPrompt(
                request.Query,
                retrievalResult.Sources);

        var client = new Client(
            apiKey: _settings.ApiKey);

        var config = new GenerateContentConfig
        {
            MaxOutputTokens = 1024
        };

        var response =
            await client.Models.GenerateContentAsync(
                model: _settings.GenerationModel,
                contents: prompt,
                config: config,
                cancellationToken: cancellationToken);

        var answer =
            response.Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .FirstOrDefault()?
                .Text;

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty answer.");
        }

        _logger.LogInformation(
            "RAG generation completed successfully.");

        return new RagResult
        {
            Answer = answer.Trim(),
            Sources = retrievalResult.Sources
        };
    }

    private static string BuildGroundedPrompt(
        string query,
        IReadOnlyList<RetrievalSource> sources)
    {
        var context = string.Join(
            System.Environment.NewLine + System.Environment.NewLine,
            sources.Select(
                source =>
                    $"[Source {source.ChunkIndex}]" +
                    System.Environment.NewLine +
                    source.Content));

        return $"""
            You are an AI assistant for a document intelligence system.

            Answer the user's question using ONLY the information contained
            in the provided document context.

            If the context does not contain enough information to answer the
            question, say that the information is not available in the
            provided documents.

            Do not invent facts.
            Do not use outside knowledge.
            Keep the answer clear and concise.

            Document context:
            {context}

            User question:
            {query}

            Answer:
            """;
    }
}