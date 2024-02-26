using DocumentFormat.OpenXml.Office.CoverPageProps;
using LLama.Abstractions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLlamaSharp.Lib
{
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