namespace BLlamaSharp.ChatGpt.Lib
{
    public class ModelSource
    {
        public ModelSource() { }
        public ModelSource(string name, string fullName, bool exists, bool isDefault)
        {
            Name = name;
            FullName = fullName;
            Exists = exists;
            IsDefault = isDefault;
        }

        public string Name { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool Exists { get; set; }
        public bool IsDefault { get; set; }


    }
}