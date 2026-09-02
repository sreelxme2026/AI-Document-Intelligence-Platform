using System.Security.Claims;
using Api.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class QueryControllerTests
{
    [Fact]
    public async Task Query_WithValidRequest_ReturnsOkWithRagResult()
    {
        var userId = Guid.NewGuid();

        var chunkId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var expectedResult = new RagResult
        {
            Answer = "The refund period is 30 days.",
            Sources =
            [
                new RetrievalSource
                {
                    DocumentChunkId = chunkId,
                    DocumentId = documentId,
                    ChunkIndex = 0,
                    Content = "Refunds are available within 30 days.",
                    SimilarityScore = 0.95
                }
            ]
        };

        var ragService = new FakeRagService(
            expectedResult);

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

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
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

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
        var userId = Guid.NewGuid();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

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

    [Fact]
    public async Task Query_CreatesQueryHistoryRecord()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "The refund period is 30 days.",
                Sources =
                [
                    new RetrievalSource
                    {
                        DocumentChunkId = Guid.NewGuid(),
                        DocumentId = Guid.NewGuid(),
                        ChunkIndex = 0,
                        Content =
                            "Refunds are available within 30 days.",
                        SimilarityScore = 0.95
                    }
                ]
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var request = new RagRequest
        {
            Query = "What is the refund period?",
            TopK = 5
        };

        await controller.Query(
            request,
            CancellationToken.None);

        Assert.True(
            historyService.WasCreateCalled);

        Assert.Equal(
            userId,
            historyService.LastUserId);

        Assert.Equal(
            request.Query,
            historyService.LastQuery);

        Assert.Equal(
            "The refund period is 30 days.",
            historyService.LastAnswer);
    }

    [Fact]
    public async Task Query_MapsRetrievedSourcesToHistorySources()
    {
        var userId = Guid.NewGuid();

        var firstChunkId = Guid.NewGuid();
        var secondChunkId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "The refund period is 30 days.",
                Sources =
                [
                    new RetrievalSource
                    {
                        DocumentChunkId = firstChunkId,
                        DocumentId = Guid.NewGuid(),
                        ChunkIndex = 0,
                        Content =
                            "Refunds are available within 30 days.",
                        SimilarityScore = 0.95
                    },
                    new RetrievalSource
                    {
                        DocumentChunkId = secondChunkId,
                        DocumentId = Guid.NewGuid(),
                        ChunkIndex = 1,
                        Content =
                            "Refund requests must be submitted within the allowed period.",
                        SimilarityScore = 0.82
                    }
                ]
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        await controller.Query(
            new RagRequest
            {
                Query = "What is the refund period?",
                TopK = 5
            },
            CancellationToken.None);

        Assert.NotNull(
            historyService.LastSources);

        Assert.Equal(
            2,
            historyService.LastSources!.Count);

        Assert.Equal(
            firstChunkId,
            historyService.LastSources[0].DocumentChunkId);

        Assert.Equal(
            0.95f,
            historyService.LastSources[0].RelevanceScore,
            precision: 5);

        Assert.Equal(
            secondChunkId,
            historyService.LastSources[1].DocumentChunkId);

        Assert.Equal(
            0.82f,
            historyService.LastSources[1].RelevanceScore,
            precision: 5);
    }

    [Fact]
    public async Task Query_WithSources_SetsHistoryAsGrounded()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Grounded answer.",
                Sources =
                [
                    new RetrievalSource
                    {
                        DocumentChunkId = Guid.NewGuid(),
                        DocumentId = Guid.NewGuid(),
                        ChunkIndex = 0,
                        Content = "Relevant document content.",
                        SimilarityScore = 0.90
                    }
                ]
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        await controller.Query(
            new RagRequest
            {
                Query = "Test grounded query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.True(
            historyService.LastIsGrounded);
    }

    [Fact]
    public async Task Query_WithoutSources_SetsHistoryAsNotGrounded()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer =
                    "I could not find relevant information in the available documents.",
                Sources = []
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        await controller.Query(
            new RagRequest
            {
                Query = "Question with no sources",
                TopK = 5
            },
            CancellationToken.None);

        Assert.False(
            historyService.LastIsGrounded);

        Assert.NotNull(
            historyService.LastSources);

        Assert.Empty(
            historyService.LastSources);
    }

    [Fact]
    public async Task Query_RecordsNonNegativeResponseTime()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        await controller.Query(
            new RagRequest
            {
                Query = "Test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.True(
            historyService.LastResponseTimeMs >= 0);
    }

    [Fact]
    public async Task Query_UsesAuthenticatedUserIdForHistory()
    {
        var authenticatedUserId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            authenticatedUserId);

        await controller.Query(
            new RagRequest
            {
                Query = "Test query",
                TopK = 5
            },
            CancellationToken.None);

        Assert.Equal(
            authenticatedUserId,
            historyService.LastUserId);
    }

    [Fact]
    public async Task Query_InvalidIdentity_ThrowsUnauthorizedAccessException()
    {
        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService();

        var controller = new QueryController(
            ragService,
            historyService);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = CreateHttpContext(
                    "not-a-guid")
            };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => controller.Query(
                new RagRequest
                {
                    Query = "Test query",
                    TopK = 5
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task Query_WhenRagFails_DoesNotCreateHistory()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new InvalidOperationException(
                "RAG generation failed."));

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Query(
                    new RagRequest
                    {
                        Query = "Test query",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "RAG generation failed.",
            exception.Message);

        Assert.False(
            historyService.WasCreateCalled);
    }

    [Fact]
    public async Task Query_WhenHistoryCreationFails_PropagatesException()
    {
        var userId = Guid.NewGuid();

        var ragService = new FakeRagService(
            new RagResult
            {
                Answer = "Test answer"
            });

        var historyService = new FakeQueryHistoryService
        {
            CreateException =
                new InvalidOperationException(
                    "History persistence failed.")
        };

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Query(
                    new RagRequest
                    {
                        Query = "Test query",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "History persistence failed.",
            exception.Message);

        Assert.True(
            historyService.WasCreateCalled);
    }

    [Fact]
    public async Task Query_WhenRagServiceThrowsArgumentException_PropagatesException()
    {
        var userId = Guid.NewGuid();

        var ragService = new ThrowingRagService(
            new ArgumentException(
                "TopK must be between 1 and 20.",
                "request"));

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => controller.Query(
                    new RagRequest
                    {
                        Query = "What is the refund policy?",
                        TopK = 21
                    },
                    CancellationToken.None));

        Assert.Equal(
            "TopK must be between 1 and 20. (Parameter 'request')",
            exception.Message);

        Assert.False(
            historyService.WasCreateCalled);
    }

    [Fact]
    public async Task Query_WhenRagServiceThrowsInvalidOperationException_PropagatesException()
    {
        var userId = Guid.NewGuid();

        var ragService = new ThrowingRagService(
            new InvalidOperationException(
                "Gemini API key is not configured."));

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => controller.Query(
                    new RagRequest
                    {
                        Query = "What is the refund policy?",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "Gemini API key is not configured.",
            exception.Message);

        Assert.False(
            historyService.WasCreateCalled);
    }

    [Fact]
    public async Task Query_WhenRagServiceThrowsUnexpectedException_PropagatesException()
    {
        var userId = Guid.NewGuid();

        var ragService = new ThrowingRagService(
            new Exception(
                "Unexpected RAG failure."));

        var historyService = new FakeQueryHistoryService();

        var controller = CreateController(
            ragService,
            historyService,
            userId);

        var exception =
            await Assert.ThrowsAsync<Exception>(
                () => controller.Query(
                    new RagRequest
                    {
                        Query = "What is the refund policy?",
                        TopK = 5
                    },
                    CancellationToken.None));

        Assert.Equal(
            "Unexpected RAG failure.",
            exception.Message);

        Assert.False(
            historyService.WasCreateCalled);
    }

    private static QueryController CreateController(
        IRagService ragService,
        IQueryHistoryService historyService,
        Guid userId)
    {
        var controller = new QueryController(
            ragService,
            historyService);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = CreateHttpContext(
                    userId.ToString())
            };

        return controller;
    }

    private static DefaultHttpContext CreateHttpContext(
        string userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(
                ClaimTypes.NameIdentifier,
                userId)
        ],
        "TestAuthentication");

        var context = new DefaultHttpContext();

        context.User =
            new ClaimsPrincipal(identity);

        return context;
    }

    private sealed class FakeRagService : IRagService
    {
        private readonly RagResult? _result;
        private readonly Exception? _exception;

        public RagRequest? LastRequest { get; private set; }

        public CancellationToken LastCancellationToken
        {
            get;
            private set;
        }

        public FakeRagService(
            RagResult result)
        {
            _result = result;
        }

        public FakeRagService(
            Exception exception)
        {
            _exception = exception;
        }

        public Task<RagResult> GenerateAnswerAsync(
            RagRequest request,
            Guid? userId,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;

            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_result!);
        }
    }

    private sealed class ThrowingRagService : IRagService
    {
        private readonly Exception _exception;

        public ThrowingRagService(
            Exception exception)
        {
            _exception = exception;
        }

        public Task<RagResult> GenerateAnswerAsync(
            RagRequest request,
            Guid? userId,
            CancellationToken cancellationToken)
        {
            return Task.FromException<RagResult>(
                _exception);
        }
    }

    private sealed class FakeQueryHistoryService
        : IQueryHistoryService
    {
        public bool WasCreateCalled { get; private set; }

        public Guid LastUserId { get; private set; }

        public string? LastQuery { get; private set; }

        public string? LastAnswer { get; private set; }

        public bool LastIsGrounded { get; private set; }

        public int LastResponseTimeMs { get; private set; }

        public IReadOnlyList<QueryHistorySourceRequest>?
            LastSources
        {
            get;
            private set;
        }

        public Exception? CreateException { get; set; }

        public Task<QueryHistoryResponse> CreateAsync(
            Guid userId,
            string query,
            string answer,
            bool isGrounded,
            int? responseTimeMs,
            IReadOnlyList<QueryHistorySourceRequest> sources)
        {
            WasCreateCalled = true;

            LastUserId = userId;
            LastQuery = query;
            LastAnswer = answer;
            LastIsGrounded = isGrounded;
            LastResponseTimeMs = responseTimeMs ?? -1;
            LastSources = sources;

            if (CreateException is not null)
            {
                throw CreateException;
            }

            return Task.FromResult(
                new QueryHistoryResponse
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Query = query,
                    Answer = answer,
                    IsGrounded = isGrounded,
                    CreatedAt = DateTime.UtcNow,
                    ResponseTimeMs = responseTimeMs,
                    Sources =
                        sources
                            .Select(source =>
                                new QueryHistorySourceResponse
                                {
                                    DocumentChunkId =
                                        source.DocumentChunkId,
                                    RelevanceScore =
                                        source.RelevanceScore
                                })
                            .ToList()
                });
        }

        public Task<QueryHistoryListResponse> GetHistoryAsync(
            Guid userId,
            QueryHistoryQueryParameters parameters)
        {
            throw new NotSupportedException();
        }

        public Task<QueryHistoryResponse?> GetByIdAsync(
            Guid userId,
            Guid historyId)
        {
            throw new NotSupportedException();
        }
    }
}