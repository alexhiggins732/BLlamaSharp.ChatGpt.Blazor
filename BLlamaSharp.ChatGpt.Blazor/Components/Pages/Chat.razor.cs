using BLlamaSharp.ChatGpt.Lib;
using LLama.Common;
using LLama;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;
using MudBlazor;
using System.Text.RegularExpressions;
using Humanizer;

namespace BLlamaSharp.ChatGpt.Blazor.Components.Pages
{

    public class ChatSession : IDisposable
    {

        public ModelParams ModelParameters = null!;
        public LLamaWeights Model = null!;
        public StatelessExecutor Executor = null!;
        public LLamaSharpChatCompletion ChatGpt = null!;
        public ChatHistory ChatHistory = null!;

        public void Dispose()
        {
            Model.Dispose();
        }
    }
    public partial class Chat : ComponentBase, IDisposable
    {
        [Inject]
        public IModelSettings ModelSettings { get; set; } = null!;

        public string UserName { get; set; } = null!;
        public string MessageText { get; set; } = null!;
        public string ChatInitializer { get; set; } = null!;
        public string ChatName { get; private set; } = null!;
        public string ChatFile { get; private set; } = null!;
        public string LogFolder { get; private set; } = null!;
        public string ChatLog { get; private set; } = null!;
        public string GettingReplyMessage { get; set; } = "Getting reply...";
        public string ModelName { get; private set; } = "";

        bool LoadedModel = false;
        string LoadingMessage = "Initializing...";
        bool loading = false;

        //getting object disposed exception when emedding the session within a component.
        public ChatSession ChatSession = new ChatSession();

        async Task SetLoadingMessage(string message)
        {
            LoadingMessage = message;
            if (string.IsNullOrEmpty(message))
                LoadedModel = true;
            StateHasChanged();
            await Task.CompletedTask;
        }

        async Task LoadingComplete()
        {
            await SetLoadingMessage("");
            loading = false;

        }


        protected override async Task OnInitializedAsync()
        {
            if (loading) return;
            loading = true;
            ChatInitializer = @"this is a conversation between a unit tester and a developer.
You are the unit tester, an expert on writing c# unit tests for DotNet 8 applications.
The developer supplies you with code, typically c# interaces.
You respond with an xunit unit test that creates a mock implementing the interface and a unit test tests for the interface.
When asked to create a mock, besure to create a mock of the interface. Make sure the mock implements all methods.
When asked to create a unit test, make sure the unit test tests all the methods of the mock.
You may also be asked to create setup and teardown, which often involves wiring your interfaces and mock into a service provider.
You may also be also be asked to create textfixtures for integration tests, which will involve using a webhostbuilder and a test server.
Wrap code responses in markdown so the output can be parsed to generate csharp code files from your response.

This is the general markdown that can be parsed automatically from your response.

``` csharp
var services= new ServiceCollection()
///TODO add services

var provider= services.BuilderServiceProvider();
return provider;

```


";



            await SetLoadingMessage("Getting Model Path...");
            var modelPath = await ModelSettings.GetModelPathAsync();
            if (string.IsNullOrEmpty(modelPath))
            {
                LoadingMessage = "No model path found. Please download a model first.";
                // Logic to prompt for download
                return;
            }
            await InitializeModel(modelPath);
            await base.OnInitializedAsync();

        }

        async Task InitializeModel(string modelPath)
        {
            if (ChatSession.Model != null)
            {
                ChatSession.Model.Dispose();
            }
            this.ModelName = Path.GetFileNameWithoutExtension(modelPath);

            await SetLoadingMessage($"Loading Model Parameters: {ModelName}...");
            ChatSession.ModelParameters = new ModelParams(modelPath);


            await SetLoadingMessage($"Loading Model: {ModelName} ...");
            ChatSession.Model = LLamaWeights.LoadFromFile(ChatSession.ModelParameters);
            var size = ChatSession.Model.ContextSize;

            await SetLoadingMessage($"Creating Executor: {ModelName} ...");
            ChatSession.Executor = new StatelessExecutor(ChatSession.Model, ChatSession.ModelParameters);

            await SetLoadingMessage($"Initializing Gpt: {ModelName} ...");
            ChatSession.ChatGpt = new LLamaSharpChatCompletion(ChatSession.Executor);


            var modelName = Path.GetFileNameWithoutExtension(modelPath).ToLower();
            var timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
            this.ChatName = $"chat-{timeStamp}-{modelName}";
            this.ChatFile = $"{ChatName}.md";
            this.LogFolder = GetLogFolder();
            this.ChatLog = Path.Combine(this.LogFolder, this.ChatFile);

            await LoadingComplete();
        }

        private string GetLogFolder()
        {
            string projectRoot = RootFolderProvider.GetRootFolder();
            // Append the 'chat' folder to the determined project root path
            var chatFolderPath = Path.Combine(projectRoot, "chat");
            // Ensure the 'chat' directory exists
            Directory.CreateDirectory(chatFolderPath);
            return chatFolderPath;
        }

        private async Task CreateNewChatAsync()
        {
            await LogChat("System", ChatInitializer);
            ChatSession.ChatHistory = ChatSession.ChatGpt.CreateNewChat(MessageText);
            MessageText = "Let's get started";
            await SendMessageAsync();
            StateHasChanged();
        }

        private async Task LogChat(string user, string messageText)
        {
            var lines = new[] {
                Environment.NewLine,
                "=".PadLeft(10, '='),
                $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]",
                user,
                messageText
            };
            await File.AppendAllLinesAsync(this.ChatLog, lines);
        }



        private async Task SendMessageAsync()
        {
            await LogChat("User", MessageText);
            ChatSession.ChatHistory.AddUserMessage(MessageText);


            MessageText = "";
            UserSendMessageButtonDisabled = UserMessageTextDisabled = true;
            GettingReplyMessage = "Getting reply...";

            await MessageOutputAsync(ChatSession.ChatHistory);
            try
            {
            readnext:
                var reply = await ChatSession.ChatGpt.GetChatMessageContentAsync(ChatSession.ChatHistory);
                await LogChat("Assistant", reply.Content);
                ChatSession.ChatHistory.AddAssistantMessage(reply.Content);
                await MessageOutputAsync(ChatSession.ChatHistory);
                var parameter = PageParameter.ParsePageParameter(reply.Content);
                if (parameter != null && parameter.PageIndex < parameter.PageCount)
                    goto readnext;

            }
            catch (Exception ex)
            {
                string message = $"I'm sorry, I'm not sure how to respond to that. {ex.Message}";
                await LogChat("Assistant", message);
                ChatSession.ChatHistory.AddAssistantMessage(message);
                await MessageOutputAsync(ChatSession.ChatHistory);
            }
            UserSendMessageButtonDisabled = UserMessageTextDisabled = false;
            GettingReplyMessage = "";
            StateHasChanged();
        }
        private bool UserMessageTextDisabled = false;
        public bool UserSendMessageButtonDisabled = false;

        private async Task MessageOutputAsync(ChatHistory history)
        {

            StateHasChanged();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            // ((IDisposable)ChatSession).Dispose();
        }
    }

    public class ChatMessage
    {
        public DateTime Created { get; set; } = DateTime.Now;
        public string UserName { get; set; }
        public string MessageText { get; set; }

        public ChatMessage(string userName, string messageText)
        {
            UserName = userName;
            MessageText = messageText;
        }
    }


    public class PageParameter
    {
        public int PageIndex { get; set; }
        public int PageCount { get; set; }

        public static PageParameter? ParsePageParameter(string message)
        {
            // Define the regex pattern to detect (x/n) format
            var pattern = @"\((\d+)/(\d+)\)";
            var regex = new Regex(pattern);
            var match = regex.Match(message);

            if (match.Success)
            {
                // Try to parse the matched groups into integers
                if (int.TryParse(match.Groups[1].Value, out int pageIndex) &&
                    int.TryParse(match.Groups[2].Value, out int pageCount))
                {
                    return new PageParameter
                    {
                        PageIndex = pageIndex,
                        PageCount = pageCount
                    };
                }
            }

            return null; // Return null if no match or parsing fails
        }
    }
}
