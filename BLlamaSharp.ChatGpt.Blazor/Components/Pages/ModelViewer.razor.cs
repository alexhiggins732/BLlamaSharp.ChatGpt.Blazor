using BLlamaSharp.ChatGpt.Lib;
using LLama.Common;
using LLama;
using LLamaSharp.SemanticKernel.ChatCompletion;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel.ChatCompletion;

using MudBlazor;
using System.Text.RegularExpressions;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Humanizer;


namespace BLlamaSharp.ChatGpt.Blazor.Components.Pages
{

    public partial class ModelViewer : ComponentBase, IDisposable
    {
        [Inject]
        IDialogService DialogService { get; set; } = null!;
        [Inject]
        IModelSettings ModelSettings { get; set; } = null!;
        [Inject]
        IModelProvider ModelProvider { get; set; } = null!;
        public string LoadingMessage { get; set; } = "Loading...";
        public bool LoadedModel { get; set; } = false;
        public List<ModelBase> AllModelsBases { get; private set; } = null!;

        private IEnumerable<ModelBase> FilteredModels()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return AllModelsBases;
            return AllModelsBases.Where(m => m.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        public string? SearchTerm { get; private set; }

        public void Dispose()
        {

        }

        protected override async Task OnInitializedAsync()
        {
            this.AllModelsBases = LoadModelBases();
            LoadedModel = true;
            await base.OnInitializedAsync();
        }

        public List<ModelBase> LoadModelBases()
        {
            var json = File.ReadAllText("models.json");
            var modelBases = new List<string>(JsonSerializer.Deserialize<List<string>>(json));
            var result = modelBases.Select(x => new ModelBase(Path.GetFileName(x), $"https://huggingface.co/{x}"))
                .OrderBy(x => x.Name)
                .ToList();

            return result;

        }

        private async Task NavigateToModelAsync(ModelBase model)
        {
            var doc = new HtmlDocument();
            var html = await new HttpClient().GetStringAsync(model.Url);
            var modelDetails = ModelDetails.ParseModelsFromHtml(ModelProvider.ModelPath, html);

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.False, // Disables the default max width
                FullWidth = true, // Optional: Makes the dialog take the full available width

                //Class = "auto-width-dialog" // Your custom CSS class to control additional styles
            };

            var parameters = new DialogParameters<ModelsDialog> {
                { x => x.Models, modelDetails },
                { x => x.Model, model}
            };

            var dialog = await DialogService.ShowAsync<ModelsDialog>("Model Details", parameters, options);
            var result = await dialog.Result;
            if (result != null)
            {
                var modelsDialogResult = (ModelsDialogResult)result.Data;
                if (modelsDialogResult is null) return;
                var selectedModel = modelsDialogResult.ModelDetails;
                if (selectedModel is null) return;
                if (modelsDialogResult.Exists)
                {
                    ModelSettings.SetDefaultModelPath(selectedModel.ModelMetaData.DisplayName);
                    return;
                }


              

                var downloadDialog = await DialogService.ShowAsync<ModelDownloadDialog>("Download Model", new DialogParameters<ModelDownloadDialog>
                    {
                        { x => x.ModelDetails, selectedModel },
                        { x => x.Model, model}
                    });
                var downloadResult = await downloadDialog.Result;
                if (downloadResult != null && downloadResult.Data is not null && downloadResult.Data is ModelDownloadDialog downloadModel)
                {

                    var downloadPath = downloadModel.DownloadPath;
                    if (File.Exists(downloadPath))
                    {
                        var modelName= Path.GetFileName(downloadPath);
                        ModelSettings.SetDefaultModelPath(modelName);
                    }
                }

            }

        }

        public void GroupAndDisplayModels(List<ModelName> models)
        {
            var grouped = models
                .GroupBy(m => m.Size)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in grouped)
            {
                //Console.WriteLine($"Size: {group.Key}");
                foreach (var model in group)
                {
                    //Console.WriteLine($"- {model.Name} (Version: {model.Version}) URL: {model.Url}");
                }
            }
        }
    }


    public class ModelsDialogResult
    {
        public ModelDetails? ModelDetails { get; set; } = null;
        public bool Exists { get; set; }
    }
}
