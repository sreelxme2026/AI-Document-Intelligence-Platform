using Application.DTOs;
using Application.Interfaces;
using Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class QueryControllerTests
{
    [Fact]
    public async Task Query_WithValidRequest_ReturnsOkWithRagResult()
    {
        var expectedResult = new RagResult
        {
            Answer = "The refund period is 30 days.",
            Sources =
            [
                new RetrievalSource
                {
                    DocumentChunkId = Guid.NewGuid(),
                    DocumentId = Guid.NewGuid(),
                    ChunkIndex = 0,
                    Content = "Refunds are available within 30 days.",
                    SimilarityScore = 0.95
                }
            ]
        };

        var ragService = new FakeRagService(expectedResult);

        var controller = new QueryController(
            ragService);

        var request = new RagRequest
        {
            Query = "What is the refund period?",
            TopK = 5
        };

        var result = await controller.Query(
            request,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<RagResult>(
            okResult.Value);

        Assert.Equal(
            expectedResult.Answer,
            response.Answer);

        Assert.Single(response.Sources);
    }

    [Fact]
    public async Task Query_PassesRequestToRagService()
    {
        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var controller = new QueryController(
            ragService);

        var request = new RagRequest
        {
            Query = "What is the refund policy?",
            TopK = 7
        };

        await controller.Query(
            request,
            CancellationToken.None);

        Assert.NotNull(
            ragService.LastRequest);

        Assert.Equal(
            "What is the refund policy?",
            ragService.LastRequest.Query);

        Assert.Equal(
            7,
            ragService.LastRequest.TopK);
    }

    [Fact]
    public async Task Query_PropagatesCancellationToken()
    {
        var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var controller = new QueryController(
            ragService);

        await controller.Query(
            new RagRequest
            {
                Query = "Test query",
                TopK = 5
            },
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            ragService.LastCancellationToken);
    }

    private sealed class FakeRagService : IRagService
    {
        private readonly RagResult _result;

        public RagRequest? LastRequest { get; private set; }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public FakeRagService(RagResult result)
        {
            _result = result;
        }

        public Task<RagResult> GenerateAnswerAsync(
            RagRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }
}