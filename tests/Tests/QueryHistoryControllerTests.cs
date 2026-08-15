using System.Security.Claims;
using Api.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Tests;

public class QueryHistoryControllerTests
{
    [Fact]
    public async Task GetHistory_WithValidUser_ReturnsOkWithHistory()
    {
        var userId = Guid.NewGuid();

        var expectedResult = new QueryHistoryListResponse
        {
            Items =
            [
                new QueryHistoryResponse
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Query = "What is the refund policy?",
                    Answer = "Refunds are available within 30 days.",
                    IsGrounded = true,
                    CreatedAt = DateTime.UtcNow,
                    ResponseTimeMs = 250
                }
            ],
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        var service = new FakeQueryHistoryService(
            expectedResult);

        var controller = CreateController(
            service,
            userId);

        var result = await controller.GetHistory(
            new QueryHistoryQueryParameters());

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<QueryHistoryListResponse>(
            okResult.Value);

        Assert.Equal(
            expectedResult.TotalCount,
            response.TotalCount);

        Assert.Single(response.Items);

        Assert.Equal(
            expectedResult.Items[0].Query,
            response.Items[0].Query);
    }

    [Fact]
    public async Task GetHistory_PassesAuthenticatedUserIdToService()
    {
        var userId = Guid.NewGuid();

        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse());

        var controller = CreateController(
            service,
            userId);

        await controller.GetHistory(
            new QueryHistoryQueryParameters
            {
                Page = 2,
                PageSize = 5
            });

        Assert.Equal(
            userId,
            service.LastUserId);

        Assert.NotNull(
            service.LastParameters);

        Assert.Equal(
            2,
            service.LastParameters!.Page);

        Assert.Equal(
            5,
            service.LastParameters.PageSize);
    }

    [Fact]
    public async Task GetById_WithExistingHistory_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        var expectedResult = new QueryHistoryResponse
        {
            Id = historyId,
            UserId = userId,
            Query = "What is the leave policy?",
            Answer = "The leave policy allows 20 days.",
            IsGrounded = true,
            CreatedAt = DateTime.UtcNow,
            ResponseTimeMs = 400
        };

        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse(),
            expectedResult);

        var controller = CreateController(
            service,
            userId);

        var result = await controller.GetById(
            historyId);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<QueryHistoryResponse>(
            okResult.Value);

        Assert.Equal(
            historyId,
            response.Id);

        Assert.Equal(
            "What is the leave policy?",
            response.Query);

        Assert.Equal(
            "The leave policy allows 20 days.",
            response.Answer);
    }

    [Fact]
    public async Task GetById_PassesAuthenticatedUserIdAndHistoryId()
    {
        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse(),
            new QueryHistoryResponse
            {
                Id = historyId,
                UserId = userId
            });

        var controller = CreateController(
            service,
            userId);

        await controller.GetById(
            historyId);

        Assert.Equal(
            userId,
            service.LastUserId);

        Assert.Equal(
            historyId,
            service.LastHistoryId);
    }

    [Fact]
    public async Task GetById_MissingHistory_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse(),
            null);

        var controller = CreateController(
            service,
            userId);

        var result = await controller.GetById(
            historyId);

        Assert.IsType<NotFoundResult>(
            result.Result);
    }

    [Fact]
    public async Task GetHistory_UsesAuthenticatedUserInsteadOfQueryParameter()
    {
        var authenticatedUserId = Guid.NewGuid();

        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse());

        var controller = CreateController(
            service,
            authenticatedUserId);

        var parameters = new QueryHistoryQueryParameters();

        await controller.GetHistory(
            parameters);

        Assert.Equal(
            authenticatedUserId,
            service.LastUserId);
    }

    [Fact]
    public async Task GetHistory_InvalidIdentity_ThrowsUnauthorizedAccessException()
    {
        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse());

        var controller = new QueryHistoryController(
            service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = CreateHttpContext(
                    "not-a-guid")
            };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => controller.GetHistory(
                new QueryHistoryQueryParameters()));
    }

    [Fact]
    public async Task GetById_InvalidIdentity_ThrowsUnauthorizedAccessException()
    {
        var service = new FakeQueryHistoryService(
            new QueryHistoryListResponse());

        var controller = new QueryHistoryController(
            service);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext = CreateHttpContext(
                    "not-a-guid")
            };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => controller.GetById(
                Guid.NewGuid()));
    }

    private static QueryHistoryController CreateController(
        FakeQueryHistoryService service,
        Guid userId)
    {
        var controller = new QueryHistoryController(
            service);

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

        context.User = new ClaimsPrincipal(identity);

        return context;
    }

    private sealed class FakeQueryHistoryService
        : IQueryHistoryService
    {
        private readonly QueryHistoryListResponse _history;
        private readonly QueryHistoryResponse? _historyItem;

        public Guid LastUserId { get; private set; }

        public Guid LastHistoryId { get; private set; }

        public QueryHistoryQueryParameters?
            LastParameters
        { get; private set; }

        public FakeQueryHistoryService(
            QueryHistoryListResponse history,
            QueryHistoryResponse? historyItem = null)
        {
            _history = history;
            _historyItem = historyItem;
        }

        public Task<QueryHistoryResponse> CreateAsync(
            Guid userId,
            string query,
            string answer,
            bool isGrounded,
            int? responseTimeMs,
            IReadOnlyList<QueryHistorySourceRequest> sources)
        {
            throw new NotSupportedException();
        }

        public Task<QueryHistoryListResponse> GetHistoryAsync(
            Guid userId,
            QueryHistoryQueryParameters parameters)
        {
            LastUserId = userId;
            LastParameters = parameters;

            return Task.FromResult(_history);
        }

        public Task<QueryHistoryResponse?> GetByIdAsync(
            Guid userId,
            Guid historyId)
        {
            LastUserId = userId;
            LastHistoryId = historyId;

            return Task.FromResult(_historyItem);
        }
    }
}