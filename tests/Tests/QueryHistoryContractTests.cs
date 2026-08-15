using Application.DTOs;
using Application.Interfaces;

namespace Tests;

public class QueryHistoryContractTests
{
    [Fact]
    public void QueryHistoryQueryParameters_ShouldHaveExpectedDefaults()
    {
        var parameters = new QueryHistoryQueryParameters();

        Assert.Equal(1, parameters.Page);
        Assert.Equal(10, parameters.PageSize);
    }

    [Fact]
    public void QueryHistoryQueryParameters_ShouldAllowPaginationValues()
    {
        var parameters = new QueryHistoryQueryParameters
        {
            Page = 3,
            PageSize = 25
        };

        Assert.Equal(3, parameters.Page);
        Assert.Equal(25, parameters.PageSize);
    }

    [Fact]
    public void QueryHistorySourceRequest_ShouldExposeExpectedProperties()
    {
        var documentChunkId = Guid.NewGuid();

        var source = new QueryHistorySourceRequest
        {
            DocumentChunkId = documentChunkId,
            RelevanceScore = 0.91f
        };

        Assert.Equal(
            documentChunkId,
            source.DocumentChunkId);

        Assert.Equal(
            0.91f,
            source.RelevanceScore);
    }

    [Fact]
    public void QueryHistorySourceResponse_ShouldExposeExpectedProperties()
    {
        var documentChunkId = Guid.NewGuid();

        var source = new QueryHistorySourceResponse
        {
            DocumentChunkId = documentChunkId,
            RelevanceScore = 0.87f
        };

        Assert.Equal(
            documentChunkId,
            source.DocumentChunkId);

        Assert.Equal(
            0.87f,
            source.RelevanceScore);
    }

    [Fact]
    public void QueryHistoryResponse_ShouldExposeExpectedProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var documentChunkId = Guid.NewGuid();

        var response = new QueryHistoryResponse
        {
            Id = id,
            UserId = userId,
            Query = "What is the refund policy?",
            Answer = "The refund period is 30 days.",
            IsGrounded = true,
            CreatedAt = createdAt,
            ResponseTimeMs = 425,
            Sources =
            [
                new QueryHistorySourceResponse
                {
                    DocumentChunkId = documentChunkId,
                    RelevanceScore = 0.94f
                }
            ]
        };

        Assert.Equal(id, response.Id);
        Assert.Equal(userId, response.UserId);

        Assert.Equal(
            "What is the refund policy?",
            response.Query);

        Assert.Equal(
            "The refund period is 30 days.",
            response.Answer);

        Assert.True(response.IsGrounded);

        Assert.Equal(
            createdAt,
            response.CreatedAt);

        Assert.Equal(
            425,
            response.ResponseTimeMs);

        Assert.Single(response.Sources);

        Assert.Equal(
            documentChunkId,
            response.Sources[0].DocumentChunkId);

        Assert.Equal(
            0.94f,
            response.Sources[0].RelevanceScore);
    }

    [Fact]
    public void QueryHistoryResponse_ShouldHaveExpectedDefaults()
    {
        var response = new QueryHistoryResponse();

        Assert.Equal(Guid.Empty, response.Id);
        Assert.Equal(Guid.Empty, response.UserId);

        Assert.Equal(
            string.Empty,
            response.Query);

        Assert.Equal(
            string.Empty,
            response.Answer);

        Assert.False(response.IsGrounded);

        Assert.Null(response.ResponseTimeMs);

        Assert.NotNull(response.Sources);
        Assert.Empty(response.Sources);
    }

    [Fact]
    public void QueryHistoryListResponse_ShouldInitializeItems()
    {
        var response = new QueryHistoryListResponse();

        Assert.NotNull(response.Items);
        Assert.Empty(response.Items);
    }

    [Fact]
    public void QueryHistoryListResponse_ShouldExposePaginationProperties()
    {
        var response = new QueryHistoryListResponse
        {
            Page = 2,
            PageSize = 10,
            TotalCount = 35,
            TotalPages = 4
        };

        Assert.Equal(2, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(35, response.TotalCount);
        Assert.Equal(4, response.TotalPages);
    }

    [Fact]
    public void QueryHistoryListResponse_ShouldStoreHistoryItems()
    {
        var item = new QueryHistoryResponse
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Query = "Test query",
            Answer = "Test answer"
        };

        var response = new QueryHistoryListResponse
        {
            Items = [item]
        };

        Assert.Single(response.Items);
        Assert.Same(item, response.Items[0]);
    }

    [Fact]
    public void IQueryHistoryService_ShouldExposeExpectedMethods()
    {
        var methods = typeof(IQueryHistoryService)
            .GetMethods();

        Assert.Contains(
            methods,
            method => method.Name == "CreateAsync");

        Assert.Contains(
            methods,
            method => method.Name == "GetHistoryAsync");

        Assert.Contains(
            methods,
            method => method.Name == "GetByIdAsync");
    }

    [Fact]
    public void IQueryHistoryService_CreateAsync_ShouldExposeExpectedParameters()
    {
        var method = typeof(IQueryHistoryService)
            .GetMethod("CreateAsync");

        Assert.NotNull(method);

        var parameters = method!.GetParameters();

        Assert.Equal(6, parameters.Length);

        Assert.Equal(
            typeof(Guid),
            parameters[0].ParameterType);

        Assert.Equal(
            typeof(string),
            parameters[1].ParameterType);

        Assert.Equal(
            typeof(string),
            parameters[2].ParameterType);

        Assert.Equal(
            typeof(bool),
            parameters[3].ParameterType);

        Assert.Equal(
            typeof(int?),
            parameters[4].ParameterType);

        Assert.Equal(
            typeof(IReadOnlyList<QueryHistorySourceRequest>),
            parameters[5].ParameterType);
    }
}