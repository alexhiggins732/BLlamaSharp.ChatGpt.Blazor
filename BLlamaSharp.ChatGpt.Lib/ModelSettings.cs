namespace BLlamaSharp.ChatGpt.Lib
{
    public interface IModelSettings
    {
        Task<string?> GetModelPathAsync();
        void SetDefaultModelPath(string path);
    }

    public class ModelSettings : IModelSettings
    {
        IModelProvider ModelProvider { get; }
        public ModelSettings(IModelProvider modelProvider, IRootFolderProvider rootFolderProvider)
        {
            ModelProvider = modelProvider;
            _settingsFilePath = Path.Join(rootFolderProvider.RootFolder, "DefaultModel.env");

        }

        private readonly string _settingsFilePath;

        public async Task<string?> GetModelPathAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Logic to prompt for download
                return null;
            }

            string path = ((await File.ReadAllTextAsync(_settingsFilePath)) ??"").Trim();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return path;
            return  null;
        }

        public void SetDefaultModelPath(string modelName)
        {
            var modelFolder = ModelProvider.ModelPath;
            var modelPath = Path.Join(modelFolder, modelName);
            if (!File.Exists(modelPath))
            {
                // Logic to prompt for download
                return;
            }
            //RootFolderProvider.EnsureFolderExists(Path.GetDirectoryName(_settingsFilePath));
            File.WriteAllText(_settingsFilePath, modelPath);
        }
    }
}
