namespace SemanticAspire.Shared;
public class OpenAIConfig
{
    public string ModelId { get; set; } = string.Empty;
    public string ChatModelId { get; set; } = string.Empty;
    public string EmbeddingModelId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
public class BingConfig
{
    public string ApiKey { get; set; } = string.Empty;
}

public class GoogleConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string SearchEngineId { get; set; } = string.Empty;
}