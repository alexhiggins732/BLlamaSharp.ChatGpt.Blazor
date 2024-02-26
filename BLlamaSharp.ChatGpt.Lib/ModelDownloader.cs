using DocumentFormat.OpenXml.Wordprocessing;
using HtmlAgilityPack;
using Humanizer;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace BLlamaSharp.ChatGpt.Lib
{
    public class DownloadProgress
    {
        public long TotalBytes { get; set; }
        public long BytesDownloaded { get; set; }
        public string? FileName { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsStarted { get; set; } = false;
        public bool IsError { get; set; } = false;
        public Exception? Exception { get; set; }
        public double DownloadPercentage => TotalBytes == 0 ? 0 : (double)BytesDownloaded / TotalBytes;
    }

    public class ModelDownloader
    {

        public static async Task<string?> DownloadModel(
            string downloadUrl, ModelDetails selectedModel, string savePath,
            Action<DownloadProgress> progressCallback,
            Func<bool> CheckForCancellation)
        {
            var client = new HttpClient();

            var downloadFile = new FileInfo(savePath);
            downloadFile.Directory.Create();

            var fileName = downloadFile.Name;
            var filePath = downloadFile.FullName;
            try
            {


                var modelUrl = downloadUrl;
                var response = await client.GetAsync(modelUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();



                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var bytesRead = 0L;
                    var buffer = new byte[81920]; // Use a smaller buffer for UI responsiveness.

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        while (true)
                        {
                            if (CheckForCancellation())
                            {
                                fileStream.Close();
                                try
                                {
                                    File.Delete(filePath);
                                }
                                catch { }

                                progressCallback(new DownloadProgress { TotalBytes = totalBytes, BytesDownloaded = bytesRead, FileName = fileName, IsCompleted = true });
                                break;
                            }
                            var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                            if (read == 0)
                            {
                                progressCallback(new DownloadProgress { TotalBytes = totalBytes, BytesDownloaded = bytesRead, FileName = fileName, IsCompleted = true });
                                break;
                            }

                            await fileStream.WriteAsync(buffer.AsMemory(0, read));
                            bytesRead += read;

                            progressCallback(new DownloadProgress
                            {
                                IsStarted = true,
                                TotalBytes = totalBytes,
                                BytesDownloaded = bytesRead,
                                FileName = fileName
                            });
                        }
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }
                progressCallback(new DownloadProgress { IsError = true, Exception = ex });
                return null;
            }
        }

        public static async Task<string?> DownloadModel1(string root, ModelDetails selectedModel, string savePath)
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
    }


    public class ModelBase
    {
        public ModelBase(string name, string url)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class ModelMetaData
    {
        public ModelMetaData()
            : this(string.Empty, string.Empty, false)
        {
        }
        public ModelMetaData(string displayName, string localPath, bool exists)
        {
            DisplayName = displayName;
            LocalPath = localPath;
            Exists = exists;
        }

        public bool Exists { get; set; } = false;
        public string DisplayName { get; set; } = null!;
        public string LocalPath { get; set; } = null!;
    }
    public class ModelDetails
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string Size { get; set; } = null!;
        public string BaseModelUrl { get; set; } = null!;



        public string QuantMethod { get; set; } = null!;
        public string Bits { get; set; } = null!;

        public string MaxRAMRequired { get; set; } = null!;
        public string UseCase { get; set; } = null!;


        // Assuming "Python" models are to be excluded, no property for Python URLs
        public string InstructUrl { get; set; } = null!;
        public string Branch { get; set; } = null!;

        public string GS { get; set; } = null!;
        public string AWQDataset { get; set; } = null!;
        public string SeqLen { get; set; } = null!;
        public string ActOrder { get; set; } = null!;
        public string Damp { get; set; } = null!;
        public string GPTQDataset { get; set; } = null!;
        public string ExLlama { get; set; } = null!;
        public string Desc { get; set; } = null!;

        public ModelMetaData ModelMetaData { get; set; } = new ModelMetaData();
        public static List<ModelDetails> ParseModelsFromHtml(string modelFolder, string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var modelsList = new List<ModelDetails>();

            // Select the last <table> element as specified
            var table = doc.DocumentNode.SelectNodes("//table").LastOrDefault();

            var props = typeof(ModelDetails).GetProperties().ToDictionary(x => x.Name, x => x);
            if (table != null)
            {
                var header = table.SelectSingleNode("thead/tr");
                var headerCells = header.SelectNodes("th").ToList();
                var headerTexts = headerCells.Select(x => x.InnerText).ToList();
                var csProps = headerTexts.ToDictionary(x => x, x => GetCsPropertyName(x));
                var csPropNames = csProps.Values.ToList();
                if (csPropNames.Any(x => !props.ContainsKey(x)))
                {
                    var invalid = csPropNames.Where(x => !props.ContainsKey(x)).ToList();
                    var modelProps = headerTexts.Select(x => $"public string {csProps[x]} {{get;set;}} = null!;").ToList();
                    var code = string.Join("\n", modelProps);
                    throw new Exception($"Invalid property name. Ensure the following properties exist:\r\n\r\n:{code}");
                }



                // Iterate through each <tr> in the <tbody>
                foreach (var row in table.SelectNodes("tbody/tr"))
                {
                    var cells = row.SelectNodes("td").ToList();

                    var model = new ModelDetails();
                    foreach (var cell in cells)
                    {
                        var cellText = cell.InnerHtml;
                        var cellIndex = cells.IndexOf(cell);
                        var propName = GetCsPropertyName(headerTexts[cellIndex]);
                        if (propName == "Name")
                        {

                            var displayName = cell.InnerText;
                            var localPath = Path.Combine(modelFolder, displayName);
                            var exists = File.Exists(localPath);
                            model.ModelMetaData = new(displayName, localPath, exists);
                        }
                        var prop = props[propName];
                        prop.SetValue(model, cellText);
                    }
                    modelsList.Add(model);
                }
            }

            return modelsList;
        }

        private static string GetCsPropertyName(string x)
        {
            x = x.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).Aggregate("", (current, c) => current + c);
            x = x.Trim();
            var name = x.Pascalize();
            return name;
        }
    }

    public class ModelName
    {

        public string Name { get; set; }
        public string Size { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }



        public static List<ModelName> ParseAndNormalizeModels(List<string> modelNames)
        {
            var models = new List<ModelName>();
            var sizePattern = @"\d+(\.\d+)?B"; // Matches '13B', '13.3B'
            var versionPattern = @"-v?(\d+(\.\d+)?(\.\d+)?)"; // Matches '-v2', '-2.2', '-2.2.1'


            foreach (var modelName in modelNames)
            {
                var sizeMatch = Regex.Match(modelName, sizePattern);
                var versionMatch = Regex.Match(modelName, versionPattern);

                models.Add(new ModelName
                {
                    Name = modelName,
                    Size = sizeMatch.Success ? sizeMatch.Value : string.Empty,
                    Version = versionMatch.Success ? versionMatch.Groups[1].Value : string.Empty,
                    Url = $"https://huggingface.co/{modelName}"
                });
            }

            return models;
        }
    }
}
