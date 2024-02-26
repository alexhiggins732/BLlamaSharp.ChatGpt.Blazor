using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace BLlamaSharp.ChatGpt.Lib
{
    public class ConsoleMessageOutputService : IMessageOutputService
    {
        public async Task OutputAsync(ChatHistory history)
        {
            // Output chat history to the console
            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");
            // Iterate through chatHistory and print messages
        }
    }
}
