using Application.Configuration;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tests;

public class RagServiceTests
{
    [Fact]
    public async Task GenerateAnswerAsync_NullRequest_ThrowsArgumentNullException()
    {
        var retrievalService = new FakeRetrievalService();

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = "test-model"
            });

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GenerateAnswerAsync(
                null!,
                CancellationToken.None));
    }

    [Fact]
    public async Task GenerateAnswerAsync_EmptyQuery_ThrowsArgumentException()
    {
        var retrievalService = new FakeRetrievalService();

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = "test-model"
            });

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GenerateAnswerAsync(
                    new RagRequest
                    {
                        Query = ""
                    },
                    CancellationToken.None));

        Assert.Equal(
            "Query cannot be null or empty. (Parameter 'request')",
            exception.Message);
    }

    [Fact]
    public async Task GenerateAnswerAsync_InvalidTopK_ThrowsArgumentException()
    {
        var retrievalService = new FakeRetrievalService();

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = "test-model"
            });

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GenerateAnswerAsync(
                    new RagRequest
                    {
                        Query = "What is the leave policy?",
                        TopK = 0
                    },
                    CancellationToken.None));

        Assert.Equal(
            "TopK must be greater than zero. (Parameter 'request')",
            exception.Message);
    }

    [Fact]
    public async Task GenerateAnswerAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        var retrievalService = new FakeRetrievalService();

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "",
                GenerationModel = "test-model"
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateAnswerAsync(
                    new RagRequest
                    {
                        Query = "What is the leave policy?",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "Gemini API key is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateAnswerAsync_MissingGenerationModel_ThrowsInvalidOperationException()
    {
        var retrievalService = new FakeRetrievalService();

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = ""
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateAnswerAsync(
                    new RagRequest
                    {
                        Query = "What is the leave policy?",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "Gemini generation model is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateAnswerAsync_NoSources_ReturnsFallbackAnswer()
    {
        var retrievalService = new FakeRetrievalService
        {
            Result = new RetrievalResult
            {
                Sources = []
            }
        };

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = "test-model"
            });

        var result =
            await service.GenerateAnswerAsync(
                new RagRequest
                {
                    Query = "What is the leave policy?",
                    TopK = 5
                },
                CancellationToken.None);

        Assert.Equal(
            "I could not find relevant information in the available documents.",
            result.Answer);

        Assert.Empty(result.Sources);
    }

    [Fact]
    public async Task GenerateAnswerAsync_NoSources_PassesQueryAndTopKToRetrieval()
    {
        var retrievalService = new FakeRetrievalService
        {
            Result = new RetrievalResult
            {
                Sources = []
            }
        };

        var service = CreateService(
            retrievalService,
            new GeminiSettings
            {
                ApiKey = "test-key",
                GenerationModel = "test-model"
            });

        await service.GenerateAnswerAsync(
            new RagRequest
            {
                Query = "What is the leave policy?",
                TopK = 7
            },
            CancellationToken.None);

        Assert.NotNull(
            retrievalService.LastRequest);

        Assert.Equal(
            "What is the leave policy?",
            retrievalService.LastRequest!.Query);

        Assert.Equal(
            7,
            retrievalService.LastRequest.TopK);
    }

    private static RagService CreateService(
        IRetrievalService retrievalService,
        GeminiSettings settings)
    {
        return new RagService(
            retrievalService,
            Options.Create(settings),
            NullLogger<RagService>.Instance);
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