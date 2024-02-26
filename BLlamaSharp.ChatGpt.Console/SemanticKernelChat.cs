using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLama;
using LLama.Common;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.ChatCompletion;
using LLama.Examples;



namespace AiCodeGenerator
{
    public class SemanticKernelChat
    {
        public static async Task Run()
        {
            string? modelPath = await UserSettings.GetModelPath();
            if (modelPath == null)
            {
                Console.WriteLine("No model path found. Please download a model first.");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This example is from: \n" +
                "https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/KernelSyntaxExamples/Example17_ChatGPT.cs");

            // Load weights into memory
            var parameters = new ModelParams(modelPath);
            using var model = LLamaWeights.LoadFromFile(parameters);
            var ex = new StatelessExecutor(model, parameters);

            var codeGpt = new LLamaSharpChatCompletion(ex);
            var codeHistory = codeGpt.CreateNewChat("this is a conversation between a unit tester and a developer.\\n"
                + "You are the unit tester, an expert on writing c# unit tests");

            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");
            codeHistory.AddUserMessage($@"Write test fror the following interface:

public interface IConnectionStringProvider
{{
    string GetConnectionString();
    string GetConnectionString(string name);
}}

");
            await MessageOutputAsync(codeHistory);
            var codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);


            codeHistory.AddUserMessage($@"Write test fror the following interface:

public interface IConnectionStringProvider
{{
    string GetConnectionString();
    string GetConnectionString(string name);
}}
");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);

            codeHistory.AddUserMessage("Okay, write the class and the test");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);


            codeHistory.AddUserMessage("Compiler complains MockConnectionStringProvider(); is not defined ");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);

            codeHistory.AddUserMessage("Compiler complains MockConnectionStringProvider does not implement GetConnectionString(string name)  ");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);


            codeHistory.AddUserMessage("Compiler complains MockConnectionStringProvider(); is not defined again. ");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);



            codeHistory.AddUserMessage(" MockConnectionStringProvider(); needs to implement  string GetConnectionString(); and string GetConnectionString(string name) and interface IConnectionStringProvider  ");
            await MessageOutputAsync(codeHistory);
            codeReply = await codeGpt.GetChatMessageContentAsync(codeHistory);
            codeHistory.AddAssistantMessage(codeReply.Content);
            await MessageOutputAsync(codeHistory);

            var chatGPT = new LLamaSharpChatCompletion(ex);

            var chatHistory = chatGPT.CreateNewChat("This is a conversation between the " +
                "assistant and the user. \n\n You are a librarian, expert about books. ");

            Console.WriteLine("Chat content:");
            Console.WriteLine("------------------------");

            chatHistory.AddUserMessage("Hi, I'm looking for book suggestions");
            await MessageOutputAsync(chatHistory);

            // First bot assistant message
            var reply = await chatGPT.GetChatMessageContentAsync(chatHistory);
            chatHistory.AddAssistantMessage(reply.Content);
            await MessageOutputAsync(chatHistory);

            // Second user message
            chatHistory.AddUserMessage("I love history and philosophy, I'd like to learn " +
                "something new about Greece, any suggestion");
            await MessageOutputAsync(chatHistory);

            // Second bot assistant message
            reply = await chatGPT.GetChatMessageContentAsync(chatHistory);
            chatHistory.AddAssistantMessage(reply.Content);
            await MessageOutputAsync(chatHistory);
        }

        /// <summary>
        /// Outputs the last message of the chat history
        /// </summary>
        private static Task MessageOutputAsync(Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory)
        {
            var message = chatHistory.Last();

            Console.WriteLine($"{message.Role}: {message.Content}");
            Console.WriteLine("------------------------");

            return Task.CompletedTask;
        }
    }
}
