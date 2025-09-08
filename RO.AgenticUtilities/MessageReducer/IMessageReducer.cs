namespace RO.AgenticUtilities.MessageReducer;

public interface IMessageReducer
{
    Task<bool> ReduceAsync(ChatHistory history, CancellationToken cancellationToken = default);
}