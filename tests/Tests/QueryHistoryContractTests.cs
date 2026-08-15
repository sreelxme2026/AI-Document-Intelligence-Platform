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
    public void QueryHistoryResponse_ShouldExposeExpectedProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var response = new QueryHistoryResponse
        {
            Id = id,
            UserId = userId,
            Query = "What is the refund policy?",
            Answer = "The refund period is 30 days.",
            CreatedAt = createdAt
        };

        Assert.Equal(id, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(
            "What is the refund policy?",
            response.Query);
        Assert.Equal(
            "The refund period is 30 days.",
            response.Answer);
        Assert.Equal(createdAt, response.CreatedAt);
    }

    [Fact]
    public void QueryHistoryResponse_ShouldHaveEmptyStringDefaults()
    {
        var response = new QueryHistoryResponse();

        Assert.Equal(string.Empty, response.Query);
        Assert.Equal(string.Empty, response.Answer);
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
            Answer = "Test answer",
            CreatedAt = DateTime.UtcNow
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
}