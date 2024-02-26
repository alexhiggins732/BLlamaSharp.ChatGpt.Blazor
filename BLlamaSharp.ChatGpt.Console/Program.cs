// See https://aka.ms/new-console-template for more information

using AiCodeGenerator;
using Microsoft.KernelMemory;

Console.ForegroundColor = ConsoleColor.White;


//await SemanticKernelChat.Run();

// TODO: Replace with your own API key
Environment.SetEnvironmentVariable("OPENAI_API_KEY", "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");
Console.WriteLine("Hello, World!");

var memoryService = new KernelMemoryBuilder()
    .WithOpenAIDefaults(Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY ENV.VAR NOT SET/"))
    .Build<MemoryServerless>();

await memoryService.ImportWebPageAsync(
    "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
    documentId: "doc02");

Console.WriteLine("Waiting for memory ingestion to complete...");
while (!await memoryService.IsDocumentReadyAsync(documentId: "doc02"))
{
    await Task.Delay(TimeSpan.FromMilliseconds(1500));
    Console.WriteLine($"[{DateTime.Now}] Waiting for memory ingestion to complete...");
}

// Ask a question
var serviceAnswer = await memoryService.AskAsync("Is there a Kernel Memory community and how can I join?");

Console.WriteLine($"\nAnswer: {serviceAnswer.Result}");


// Connected to the memory service running locally
var memory = new MemoryWebClient("http://localhost:9001/");

// Import a web page
await memory.ImportWebPageAsync(
    "https://raw.githubusercontent.com/microsoft/kernel-memory/main/README.md",
    documentId: "doc02");

// Wait for ingestion to complete, usually 1-2 seconds
Console.WriteLine("Waiting for memory ingestion to complete...");
while (!await memory.IsDocumentReadyAsync(documentId: "doc02"))
{
    await Task.Delay(TimeSpan.FromMilliseconds(1500));
    Console.WriteLine($"[{DateTime.Now}] Waiting for memory ingestion to complete...");
}

// Ask a question
var answer = await memory.AskAsync("Is there a Kernel Memory community and how can I join?");

Console.WriteLine($"\nAnswer: {answer.Result}");