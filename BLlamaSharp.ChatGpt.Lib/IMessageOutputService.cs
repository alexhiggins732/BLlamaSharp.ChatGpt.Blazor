using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace BLlamaSharp.ChatGpt.Lib
{
    public interface IMessageOutputService
    {
        Task OutputAsync(ChatHistory history);
    }
}
