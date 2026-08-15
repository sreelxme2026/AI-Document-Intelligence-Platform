namespace Application.Configuration;

public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;

    public string EmbeddingModel { get; set; } = "gemini-embedding-2";

    public int OutputDimension { get; set; } = 1536;
}