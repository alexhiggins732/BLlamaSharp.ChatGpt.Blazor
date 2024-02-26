using HtmlAgilityPack;
using Spectre.Console;
using System.Text.Json;


namespace LLama.Examples;

public class ModelData
{
    public string Root { get; set; }
    public List<Model> Models { get; set; }
}
public class Model
{
    public string Name { get; set; }
    public string QuantMethod { get; set; }
    public int Bits { get; set; }
    public string Size { get; set; }
    public string MaxRAMRequired { get; set; }
    public string UseCase { get; set; }
}

internal static class UserSettings
{
    private static string SettingsFilePath = Path.Join(AppContext.BaseDirectory, "DefaultModel.env");

    private static async Task<string?> ReadDefaultModelPath()
    {
        if (!File.Exists(SettingsFilePath))
            return null;

        string path = (await File.ReadAllTextAsync(SettingsFilePath)).Trim();
        if (!File.Exists(path))
            return null;

        return path;
    }

    private static async Task<string?> PromptUserForDownload(string savePath)
    {
        var jsonString = File.ReadAllText("models.json");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, // This can help if the case doesn't match
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Use this if your JSON properties are camelCase
        };

        var modelData = JsonSerializer.Deserialize<ModelData>(jsonString, options);
        var models = modelData.Models;
        var table = new Table();
        table.AddColumn("Index");
        table.AddColumn("Name");
        table.AddColumn("Quant Method");
        table.AddColumn("Bits");
        table.AddColumn("Size");
        table.AddColumn("Max RAM Required");
        table.AddColumn("Use Case");

        for (var i = 1; i <= modelData.Models.Count; i++)
        {
            var model = modelData.Models[i - 1];
            table.AddRow(i.ToString(), model.Name, model.QuantMethod, model.Bits.ToString(), model.Size, model.MaxRAMRequired, model.UseCase);
        }


        AnsiConsole.Render(table);

        //TODO: Prompt user for download. Return false if they cancel or download the model, save the path in "DefaultModel.env" and return true
        // Prompt user for selection
        var selectedIndex = AnsiConsole.Prompt(
            new TextPrompt<int>("Enter the index of the model to download (or 0 to cancel):")
                .PromptStyle("white")
                .Validate(input =>
                {
                    if (input >= 0 && input <= models.Count)
                        return ValidationResult.Success();
                    return ValidationResult.Error("[red]Please enter a valid index![/]");
                }));

        // If the user selects 0, return false
        if (selectedIndex == 0)
        {
            return null;
        }

        // Assuming you have a method to handle the download given a Model object
        var selectedModel = models[selectedIndex - 1];
        var downloadPath = await DownloadModel(modelData.Root, selectedModel, savePath); // You need to implement this method

        if (!string.IsNullOrEmpty(downloadPath))
        {
            File.WriteAllText(SettingsFilePath, downloadPath);
            return downloadPath;
        }

        return null;
    }

    private static async Task<string?> DownloadModel(string root, Model selectedModel, string savePath)
    {
        var client = new HttpClient();
        var modelUrl = $"{root}/{selectedModel.Name}";
        var response = await client.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        var downloadFolderPath = Path.Combine(savePath);
        Directory.CreateDirectory(downloadFolderPath); // Ensure the directory exists
        var filePath = Path.Combine(downloadFolderPath, selectedModel.Name);


        AnsiConsole.MarkupLine($"Beginning download of [bold]{selectedModel.Name}[/]...");
        Console.ForegroundColor = ConsoleColor.Green;
        using (var stream = await response.Content.ReadAsStreamAsync())
        {
            const int bufferSize = 1000 * 1000; // This buffer size is used by FileStream internally anyway.
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var bytesRead = 0L;
            var buffer = new byte[bufferSize];
            var isMoreToRead = true;

            var last = "";

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
            {
                do
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        isMoreToRead = false;
                    }
                    else
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, read));
                        bytesRead += read;

                        if (totalBytes != -1)
                        {
                            var percentComplete = (double)bytesRead / totalBytes;

                            var spinChar = (int)Math.Floor((double)DateTime.Now.Millisecond / 250) switch
                            {
                                0 => '|',
                                1 => '/',
                                2 => '-',
                                3 => '\\',
                                _ => '|'
                            };
                            var message = $" ==> {spinChar} Download progress: {percentComplete.ToString("P")} ({(bytesRead / 1000.0 / 1000).ToString("N2")} MiB/  {(totalBytes / 1000.0 / 1000).ToString("N2")} MiB)";
                            if (!string.IsNullOrEmpty(last))
                            {

                                Console.Write("".PadLeft(last.Length, '\b'));
                            }
                            last = message;
                            Console.ForegroundColor = ConsoleColor.Green;
                            AnsiConsole.Markup(last);
                        }
                    }
                }
                while (isMoreToRead);
            }
        }
        Console.ResetColor();
        AnsiConsole.MarkupLine($"[bold]Download of {selectedModel.Name} complete.[/]");

        return filePath; // Return the local path where the model was saved
    }

    private static void WriteDefaultModelPath(string path)
    {
        File.WriteAllText(SettingsFilePath, path);
    }

    public async static Task<string?> GetModelPath(bool alwaysPrompt = false)
    {
        var path = await ReadDefaultModelPath();
        if (alwaysPrompt || (string.IsNullOrEmpty(path) || !File.Exists(path)))
        {
            path = await PromptUserForPath();
        }

        return path;
    }

    private static async Task<string?> PromptUserForPath()
    {
        return await PromptUserForDownload(Path.Combine(Path.GetFullPath("."), "models"));

    }

    //private static string PromptUserForPathWithDefault(string defaultPath)
    //{
    //    return AnsiConsole.Prompt(
    //        new TextPrompt<string>("Please input your model path (or ENTER for default):")
    //           .DefaultValue(defaultPath)
    //           .PromptStyle("white")
    //           .Validate(File.Exists, "[red]ERROR: invalid model file path - file does not exist[/]")
    //    );
    //}
}
