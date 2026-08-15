using Application.Configuration;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tests;

public class RagServiceIntegrationTests
{
    private static bool IntegrationTestsEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithRealGemini_ReturnsAnswerAndSources()
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

        var generationModel =
            Environment.GetEnvironmentVariable(
                "GEMINI_GENERATION_MODEL");

        if (string.IsNullOrWhiteSpace(generationModel))
        {
            generationModel = "gemini-3.5-flash-lite";
        }

        var retrievalService =
            new FakeRetrievalService
            {
                Result = new RetrievalResult
                {
                    Sources =
                    [
                        new RetrievalSource
                        {
                            DocumentChunkId = Guid.NewGuid(),
                            DocumentId = Guid.NewGuid(),
                            ChunkIndex = 0,
                            Content =
                                "The company provides 20 days of annual leave to each full-time employee.",
                            PageNumber = 1,
                            SimilarityScore = 0.95
                        },
                        new RetrievalSource
                        {
                            DocumentChunkId = Guid.NewGuid(),
                            DocumentId = Guid.NewGuid(),
                            ChunkIndex = 1,
                            Content =
                                "Employees must submit leave requests through the employee portal.",
                            PageNumber = 2,
                            SimilarityScore = 0.88
                        }
                    ]
                }
            };

        var service = new RagService(
            retrievalService,
            Options.Create(
                new GeminiSettings
                {
                    ApiKey = apiKey,
                    GenerationModel = generationModel
                }),
            NullLogger<RagService>.Instance);

        var result =
            await service.GenerateAnswerAsync(
                new RagRequest
                {
                    Query = "How many days of annual leave does a full-time employee receive?",
                    TopK = 2
                },
                CancellationToken.None);

        Assert.NotNull(result);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Answer));

        Assert.Equal(
            2,
            result.Sources.Count);

        Assert.Contains(
            result.Sources,
            source =>
                source.Content.Contains(
                    "20 days of annual leave",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            "How many days of annual leave does a full-time employee receive?",
            retrievalService.LastRequest!.Query);

        Assert.Equal(
            2,
            retrievalService.LastRequest.TopK);
    }

    [Fact]
    public async Task GenerateAnswerAsync_WithRealGemini_PreservesRetrievedSources()
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

        var generationModel =
            Environment.GetEnvironmentVariable(
                "GEMINI_GENERATION_MODEL");

        if (string.IsNullOrWhiteSpace(generationModel))
        {
            generationModel = "gemini-3.5-flash-lite";
        }

        var source = new RetrievalSource
        {
            DocumentChunkId = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            ChunkIndex = 3,
            Content =
                "The office is open from 9 AM to 5 PM Monday through Friday.",
            PageNumber = 4,
            SimilarityScore = 0.91
        };

        var retrievalService =
            new FakeRetrievalService
            {
                Result = new RetrievalResult
                {
                    Sources = [source]
                }
            };

        var service = new RagService(
            retrievalService,
            Options.Create(
                new GeminiSettings
                {
                    ApiKey = apiKey,
                    GenerationModel = generationModel
                }),
            NullLogger<RagService>.Instance);

        var result =
            await service.GenerateAnswerAsync(
                new RagRequest
                {
                    Query = "What are the office hours?",
                    TopK = 1
                },
                CancellationToken.None);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.Answer));

        Assert.Single(result.Sources);

        Assert.Equal(
            source.DocumentChunkId,
            result.Sources[0].DocumentChunkId);

        Assert.Equal(
            source.DocumentId,
            result.Sources[0].DocumentId);

        Assert.Equal(
            source.ChunkIndex,
            result.Sources[0].ChunkIndex);

        Assert.Equal(
            source.Content,
            result.Sources[0].Content);

        Assert.Equal(
            source.PageNumber,
            result.Sources[0].PageNumber);

        Assert.Equal(
            source.SimilarityScore,
            result.Sources[0].SimilarityScore);
    }

    private sealed class FakeRetrievalService
        : IRetrievalService
    {
        public RetrievalRequest? LastRequest { get; private set; }

        public RetrievalResult Result { get; set; }
            = new RetrievalResult();

        public Task<RetrievalResult> RetrieveAsync(
            RetrievalRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(Result);
        }
    }
}