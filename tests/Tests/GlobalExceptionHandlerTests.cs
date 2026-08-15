using Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ArgumentException_ReturnsBadRequest()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentException("Invalid query."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ArgumentNullException_ReturnsBadRequest()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new ArgumentNullException("request"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status400BadRequest,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new UnauthorizedAccessException(
                "Invalid user identity."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_InvalidOperationException_ReturnsInternalServerError()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException(
                "Gemini API key is not configured."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_UnexpectedException_ReturnsInternalServerError()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new Exception("Unexpected failure."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsProblemDetailsContentType()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        await handler.TryHandleAsync(
            context,
            new ArgumentException("Invalid query."),
            CancellationToken.None);

        Assert.Equal(
            "application/problem+json",
            context.Response.ContentType);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsProblemDetailsWithExpectedStatus()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        await handler.TryHandleAsync(
            context,
            new ArgumentException("Invalid query."),
            CancellationToken.None);

        context.Response.Body.Position = 0;

        var response =
            await System.Text.Json.JsonSerializer.DeserializeAsync<
                Dictionary<string, object>>(
                context.Response.Body);

        Assert.NotNull(response);

        Assert.True(
            response!.ContainsKey("status"));
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsTrueWhenExceptionIsHandled()
    {
        var handler = CreateHandler();

        var context = CreateHttpContext("/api/v1/query");

        var handled = await handler.TryHandleAsync(
            context,
            new Exception("Test exception."),
            CancellationToken.None);

        Assert.True(handled);
    }

    private static GlobalExceptionHandler CreateHandler()
    {
        return new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext(
        string path)
    {
        var context = new DefaultHttpContext();

        context.Request.Path = path;

        context.Response.Body =
            new MemoryStream();

        return context;
    }
}