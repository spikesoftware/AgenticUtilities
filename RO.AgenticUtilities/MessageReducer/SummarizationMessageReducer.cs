using Microsoft.Extensions.Logging;


namespace RO.AgenticUtilities.MessageReducer;

/// <summary>
/// An implementation of IMessageReducer used to identify when the chathistory has exceeded
/// a threshold and reduce the chathistory by generating a summary of the oldest chat history.
/// </summary>
public class SummarizingMessageReducer : IMessageReducer
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ITokenizer _tokenizer;
    private readonly MessageReducerOptions _options;
    private readonly ILogger<SummarizingMessageReducer> _logger;

    public SummarizingMessageReducer(
        IChatCompletionService chatCompletionService,
        ITokenizer tokenizer,
        MessageReducerOptions options,
        ILogger<SummarizingMessageReducer> logger)
    {
        _tokenizer = tokenizer
            ?? throw new ArgumentNullException(nameof(tokenizer));
        _chatCompletionService = chatCompletionService
            ?? throw new ArgumentException(nameof(chatCompletionService));
        _options = options
            ?? throw new ArgumentNullException(nameof(options));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Given a ChatHistory, reduce if over the defined threshold
    /// </summary>
    /// <param name="history"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> ReduceAsync(
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        int totalTokens = CountTokens(history);
        if (!NeedsReduction(totalTokens))
        {
            _logger.LogDebug(
                "Token count {TokenCount} under threshold {Threshold}; skipping.",
                totalTokens,
                _options.MaxContextTokens - _options.BufferTokens);
            return false;
        }

        var toSummarize = SelectTurns(history);
        var prompt = BuildSummarizationPrompt(toSummarize);

        string summary = await GenerateSummaryAsync(
            prompt,
            cancellationToken);

        ApplySummary(history, toSummarize.Count, summary);

        return true;
    }

    /// <summary>
    /// Counts the total number of tokens in the provided chat history.Token counting
    /// </summary>
    /// <remarks>Each message in the chat history is processed by combining the role and content of the
    /// message into a single string. The token count is calculated based on the combined strings, considering
    /// pre-tokenization and normalization.</remarks>
    /// <param name="history">The chat history containing messages to be tokenized and counted.</param>
    /// <returns>The total number of tokens in the chat history.</returns>
    protected virtual int CountTokens(ChatHistory history) =>
        history.ToArray()
            .Select(m => $"{m.Role}: {m.Content}")
            .Sum(text => _tokenizer.CountTokens(
                text.AsSpan(),
                considerPreTokenization: true,
                considerNormalization: true));

    /// <summary>
    /// Determines whether the total number of tokens exceeds the allowable threshold.Threshold check
    /// </summary>
    /// <remarks>The threshold is calculated as the maximum context tokens minus the buffer tokens, as defined
    /// in the options.</remarks>
    /// <param name="totalTokens">The total number of tokens to evaluate.</param>
    /// <returns><see langword="true"/> if the total number of tokens exceeds the threshold; otherwise, <see langword="false"/>.</returns>
    protected virtual bool NeedsReduction(int totalTokens) =>
        totalTokens > (_options.MaxContextTokens - _options.BufferTokens);

    /// <summary>
    /// Selects and formats the oldest turns from the chat history, up to a specified count.
    /// </summary>
    /// <remarks>This method retrieves the oldest turns from the chat history, formats each turn with its role
    /// and content, and returns the result as a read-only list. The number of turns selected is  determined by the
    /// collapse turn count specified in the options.</remarks>
    /// <param name="history">The chat history from which turns are selected. Cannot be null.</param>
    /// <returns>A read-only list of strings representing the selected turns, formatted as "[Role] Content". The list will
    /// contain at most the number of turns specified by the collapse turn count.</returns>
    protected virtual IReadOnlyList<string> SelectTurns(ChatHistory history) =>
        history.ToArray()
               .Take(_options.CollapseTurnCount)
               .Select(m => $"[{m.Role}] {m.Content}")
               .ToList();

    /// <summary>
    /// Constructs a summarization prompt based on the provided conversation turns.
    /// </summary>
    /// <param name="turns">A read-only list of conversation turns to be summarized. Each turn represents a distinct part of the
    /// conversation.</param>
    /// <returns>A string containing the summarization prompt, formatted as a concise bullet list request followed by the
    /// provided conversation turns.</returns>
    protected virtual string BuildSummarizationPrompt(IReadOnlyList<string> turns) =>
        "Summarize the key facts and decisions from these earlier conversation turns into a concise bullet list:\n\n"
        + string.Join("\n\n", turns);

    /// <summary>
    /// Generates a concise summary based on the provided user prompt.
    /// </summary>
    /// <remarks>This method is designed to be overridden in derived classes to customize the summarization
    /// behavior.</remarks>
    /// <param name="userPrompt">The input text or prompt for which a summary is to be generated.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated summary as a trimmed
    /// string. If no summary is generated, the result will be "• (No summary generated)".</returns>
    protected virtual async Task<string> GenerateSummaryAsync(
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var summaryHistory = new ChatHistory();
        summaryHistory.Add(new ChatMessageContent(
            AuthorRole.System,
            "You are a concise summarization assistant."));
        summaryHistory.Add(new ChatMessageContent(
            AuthorRole.User,
            userPrompt));

        var settings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        };

        var result = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory: summaryHistory,
            executionSettings: settings,
            cancellationToken: cancellationToken);

        return result.Content?.Trim()
               ?? "• (No summary generated)";
    }

    /// <summary>
    /// Updates the chat history by removing a specified number of messages and inserting a summary of the removed
    /// content.
    /// </summary>
    /// <remarks>This method is intended to manage the size of the chat history by summarizing earlier
    /// messages.  The summary is added as a new message at the beginning of the history, attributed to the
    /// assistant.</remarks>
    /// <param name="history">The chat history to modify. This collection will be updated in place.</param>
    /// <param name="removeCount">The number of messages to remove from the beginning of the chat history. Must be non-negative and less than or
    /// equal to the number of messages in <paramref name="history"/>.</param>
    /// <param name="summaryText">The summary text to insert, representing the content of the removed messages. This text will be prefixed with a
    /// standard summary header.</param>
    protected virtual void ApplySummary(
        ChatHistory history,
        int removeCount,
        string summaryText)
    {
        history.RemoveRange(0, removeCount);
        history.Insert(0, new ChatMessageContent(
            AuthorRole.Assistant,
            $"[Summary of earlier conversation]\n• {summaryText.Replace("\n", "\n• ")}"));

        _logger.LogInformation(
            "Pruned {Count} turns, inserted summary.",
            removeCount);
    }

}
