namespace BLlamaSharp.ChatGpt.Lib
{
    public interface IRootFolderProvider
    {
        string RootFolder { get; set; }
    }
    /// <summary>
    /// Provides the storage root folder for the application. Returns AppContext.BaseDirectory if not set and not running in a debug mode.
    /// </summary>
    /// <returns>
    /// The csproject root folder if running in a debug mode, otherwise AppContext.BaseDirectory
    /// Can be set by calling SetRootFolder
    /// </returns>
    public class RootFolderProvider : IRootFolderProvider
    {
        public string RootFolder { get; set; }
        public RootFolderProvider()
        {
            RootFolder = GetRootFolder();
        }
        static string? _rootFolder;

        public static void SetRootFolder(string rootFolder)
        {
            _rootFolder = rootFolder;
        }
        public static string GetRootFolder()
        {
            if (!string.IsNullOrEmpty(_rootFolder))
                return _rootFolder;

            // Start with the base directory of the application
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Try to find the 'bin' directory in the path which indicates we might be running in a debug or release mode within the IDE or via 'dotnet run'
            var binIndex = currentDirectory.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

            string projectRoot;
            if (binIndex != -1)
            {
                // If 'bin' is found, assume we are running within the IDE or via 'dotnet run'. Trim the path to get the project root
                projectRoot = currentDirectory.Substring(0, binIndex);
            }
            else
            {
                // If no 'bin' directory is found in the path, assume the application is running as a standalone executable
                projectRoot = currentDirectory;
            }
            return projectRoot;
        }

    }
}