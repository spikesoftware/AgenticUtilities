namespace RO.AgenticUtilities;

public interface ITokenizer
{
    int CountTokens(
        ReadOnlySpan<char> span,
        bool considerPreTokenization,
        bool considerNormalization);
}