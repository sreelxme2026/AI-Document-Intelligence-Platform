using Application.DTOs;

namespace Tests;

public class RagContractTests
{
    [Fact]
    public void RagRequest_DefaultValues_AreCorrect()
    {
        var request = new RagRequest();

        Assert.Equal(
            string.Empty,
            request.Query);

        Assert.Equal(
            5,
            request.TopK);
    }

    [Fact]
    public void RagRequest_CanStoreQueryAndTopK()
    {
        var request = new RagRequest
        {
            Query = "What is the leave policy?",
            TopK = 8
        };

        Assert.Equal(
            "What is the leave policy?",
            request.Query);

        Assert.Equal(
            8,
            request.TopK);
    }

    [Fact]
    public void RagResult_DefaultValues_AreCorrect()
    {
        var result = new RagResult();

        Assert.Equal(
            string.Empty,
            result.Answer);

        Assert.Empty(
            result.Sources);
    }

    [Fact]
    public void RagResult_CanStoreAnswerAndSources()
    {
        var documentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();

        var source = new RetrievalSource
        {
            DocumentChunkId = chunkId,
            DocumentId = documentId,
            ChunkIndex = 2,
            Content = "The annual leave policy provides 20 days.",
            PageNumber = 4,
            SimilarityScore = 0.91
        };

        var result = new RagResult
        {
            Answer =
                "Employees receive 20 days of annual leave.",
            Sources =
            [
                source
            ]
        };

        Assert.Equal(
            "Employees receive 20 days of annual leave.",
            result.Answer);

        var returnedSource =
            Assert.Single(result.Sources);

        Assert.Equal(
            chunkId,
            returnedSource.DocumentChunkId);

        Assert.Equal(
            documentId,
            returnedSource.DocumentId);

        Assert.Equal(
            2,
            returnedSource.ChunkIndex);

        Assert.Equal(
            "The annual leave policy provides 20 days.",
            returnedSource.Content);

        Assert.Equal(
            4,
            returnedSource.PageNumber);

        Assert.Equal(
            0.91,
            returnedSource.SimilarityScore,
            precision: 5);
    }
}