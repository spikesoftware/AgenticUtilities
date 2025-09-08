namespace RO.AgenticUtilities.MessageReducer;

public record MessageReducerOptions
{
    public int MaxContextTokens { get; init; }
    public int BufferTokens { get; init; }
    public int CollapseTurnCount { get; init; }
}