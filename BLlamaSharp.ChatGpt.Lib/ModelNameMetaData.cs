namespace BLlamaSharp.ChatGpt.Lib
{
    public class ModelNameMetaData
    {
        public string ModelName { get; set; }
        public string Version { get; set; }
        public string Configuration { get; set; }
        public string Capacity { get; set; }

        // Constructor to initialize the ModelRecord from a file name
        public ModelNameMetaData(string fileName)
        {
            // Parsing logic will go here
            // For simplicity, we'll assume a basic structure and split by '-'
            var parts = fileName.Split('-');
            ModelName = parts.Length > 0 ? parts[0] : string.Empty;
            Version = parts.Length > 1 ? parts[1] : string.Empty;
            Configuration = parts.Length > 2 ? parts[2] : string.Empty;
            Capacity = parts.Length > 3 ? parts[3] : string.Empty;
            // Note: This is a simplified parser and may need adjustment to fit actual naming conventions
        }
    }

}
