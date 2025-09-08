namespace RO.AgenticUtilities.Tests.Fakes
{
    internal class FakeTokenizer : ITokenizer
    {
        public FakeTokenizer()
        {
        }

        public int CountTokens(
            ReadOnlySpan<char> span,
            bool considerPreTokenization,
            bool considerNormalization)
        {
            return span.Length;
        }
    }
}
