namespace DevTrack.Infrastructure.AI;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "gemma4";
    public int RequestTimeoutSeconds { get; init; } = 180;
}
