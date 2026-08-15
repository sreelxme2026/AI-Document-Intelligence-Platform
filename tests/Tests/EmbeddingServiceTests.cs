using Application.Configuration;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ThrowsArgumentException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(
            dbContext,
            new GeminiSettings
            {
                ApiKey = "test-key",
                EmbeddingModel = "gemini-embedding-2"
            });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GenerateEmbeddingAsync(""));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(
            dbContext,
            new GeminiSettings
            {
                ApiKey = "",
                EmbeddingModel = "gemini-embedding-2"
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateEmbeddingAsync("test text"));

        Assert.Equal(
            "Gemini API key is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_MissingModel_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();

        var service = CreateService(
            dbContext,
            new GeminiSettings
            {
                ApiKey = "test-key",
                EmbeddingModel = ""
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateEmbeddingAsync("test text"));

        Assert.Equal(
            "Gemini embedding model is not configured.",
            exception.Message);
    }

    [Fact]
    public async Task GenerateEmbeddingsForDocumentAsync_NoChunks_ThrowsInvalidOperationException()
    {
        var documentId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();

        var service = CreateService(
            dbContext,
            new GeminiSettings
            {
                ApiKey = "",
                EmbeddingModel = "gemini-embedding-2"
            });

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GenerateEmbeddingsForDocumentAsync(
                    documentId));

        Assert.Equal(
            $"No document chunks were found for document {documentId}.",
            exception.Message);
    }

    private static EmbeddingService CreateService(
        AppDbContext dbContext,
        GeminiSettings settings)
    {
        return new EmbeddingService(
            dbContext,
            Options.Create(settings),
            NullLogger<EmbeddingService>.Instance);
    }

    private static AppDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"EmbeddingServiceTests-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }
}