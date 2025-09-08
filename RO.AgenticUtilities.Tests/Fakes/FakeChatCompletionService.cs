using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace RO.AgenticUtilities.MessageReducer.Tests;

public class FakeChatCompletionService : IChatCompletionService
{
    private readonly string? _summary;

    public IReadOnlyDictionary<string, object?> Attributes => throw new NotImplementedException();

    public FakeChatCompletionService(string? summary)
    {
        _summary = summary;
    }

    public Task<ChatMessageContent> GetChatMessageContentAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? settings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ChatMessageContent(
            AuthorRole.Assistant,
            _summary ?? string.Empty);

        return Task.FromResult(response);
    }

    // Optional: implement other interface members if needed
    public Task<string> GetChatMessageAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? settings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_summary ?? string.Empty);
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? settings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ChatMessageContent(
            AuthorRole.Assistant,
            _summary ?? string.Empty);

        return Task.FromResult<IReadOnlyList<ChatMessageContent>>(new[] { response });
    }

    public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var response = new ChatMessageContent(
            AuthorRole.Assistant,
            _summary ?? string.Empty);

        return Task.FromResult<IReadOnlyList<ChatMessageContent>>(new[] { response });
    }

    IAsyncEnumerable<StreamingChatMessageContent> IChatCompletionService.GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings, Kernel? kernel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}