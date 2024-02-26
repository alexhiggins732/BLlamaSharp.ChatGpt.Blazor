using Microsoft.Extensions.Configuration;

namespace BLlamaSharp.ChatGpt.Lib
{
    public interface IModelProvider
    {
        string ModelPath { get; set; }
        Dictionary<string, ModelSource> DownloadedModels { get; set; }
    }
    public class ModelProvider : IModelProvider
    {
        public string ModelPath { get; set; }
        public Dictionary<string, ModelSource> DownloadedModels { get; set; }
        public ModelProvider()
        {
            ModelPath = Path.Combine(RootFolderProvider.GetRootFolder(), "Models");
            Directory.CreateDirectory(ModelPath);
            DownloadedModels = new();
        }

        public ModelProvider(IConfiguration configuration)
            : this()
        {
            configuration.GetSection("Models").Bind(this);
            DownloadedModels ??= new();
            if (!string.IsNullOrEmpty(ModelPath))
            {
                var existing = new List<Dictionary<string, ModelSource>>();
                var di = new DirectoryInfo(ModelPath);
                if (di.Exists)
                {
                    var sources = di.GetFiles().Select(x => new ModelSource(x.Name, x.FullName, true, false));
                    sources.Where(x => !DownloadedModels.ContainsKey(x.Name))
                        .ToList()
                        .ForEach(x => DownloadedModels.Add(x.Name, x));
                }
            }

        }
    }
}