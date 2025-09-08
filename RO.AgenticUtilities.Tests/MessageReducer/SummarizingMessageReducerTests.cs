using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using RO.AgenticUtilities.Tests.Fakes;

namespace RO.AgenticUtilities.MessageReducer.Tests;

[TestClass]
public class SummarizingMessageReducerTests
{
    private MessageReducerOptions _options = null!;
    private IChatCompletionService _chatService = null!;
    private ITokenizer _tokenizer = null!;
    private SummarizingMessageReducer _reducer = null!;
    private readonly NullLogger<SummarizingMessageReducer> _logger = new();

    [TestInitialize]
    public void Setup()
    {
        _options = new MessageReducerOptions
        {
            MaxContextTokens = 50,
            BufferTokens = 10,
            CollapseTurnCount = 3
        };

        _chatService = new FakeChatCompletionService("fake summary");
        _tokenizer = new FakeTokenizer();

        _reducer = new SummarizingMessageReducer(
            _chatService,
            _tokenizer,
            _options,
            _logger);
    }

    [TestMethod]
    public async Task ReduceAsync_UnderThreshold_ReturnsFalse_AndLeavesHistoryUnchanged()
    {
        var history = new ChatHistory();
        history.Add(new ChatMessageContent(AuthorRole.User, "hello"));
        history.Add(new ChatMessageContent(AuthorRole.Assistant, "world"));

        bool reduced = await _reducer.ReduceAsync(history, CancellationToken.None);

        Assert.IsFalse(reduced);
        Assert.AreEqual(2, history.ToArray().Length);
    }

    [TestMethod]
    public async Task ReduceAsync_OverThreshold_ReturnsTrue_AndPrependsSummary()
    {
        // Make token count exceed threshold
        _tokenizer = new FakeTokenizer();
        _reducer = new SummarizingMessageReducer(
            _chatService,
            _tokenizer,
            _options,
            _logger);

        var history = new ChatHistory();
        for (int i = 0; i < 5; i++)
        {
            var role = i % 2 == 0 ? AuthorRole.User : AuthorRole.Assistant;
            var content = $"turn{i}";
            history.Add(new ChatMessageContent(role, content));
        }

        bool reduced = await _reducer.ReduceAsync(history, CancellationToken.None);
        var messages = history.ToArray();

        Assert.IsTrue(reduced);
        // After collapsing 3 turns and inserting 1 summary, count = 5 - 3 + 1 = 3
        Assert.AreEqual(3, messages.Length);

        StringAssert.StartsWith(messages[0].Content, "[Summary of earlier conversation]");
        StringAssert.Contains(messages[0].Content, "fake summary");
    }

}
