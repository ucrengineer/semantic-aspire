namespace SemanticAspire.Shared;
public class OpenAIConfig
{
    public required string ModelId { get; set; }
    public required string ChatModelId { get; set; }
    public required string EmbeddingModelId { get; set; }
    public required string ApiKey { get; set; }
}

public class BingConfig
{
    public required string ApiKey { get; set; }
}

public class GoogleConfig
{
    public required string ApiKey { get; set; }
    public required string SearchEngineId { get; set; }
}