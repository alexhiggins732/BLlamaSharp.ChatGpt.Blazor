using Xunit;
using FluentAssertions;
using System.Text.Json;
using Humanizer;
using System.ComponentModel.DataAnnotations;


namespace BLlamaSharp.ChatGpt.Lib.Tests
{
    public class SearchTests()
    {
        [Fact]
        public void LoadData()
        {
            // Arrange
            var modelsJson = File.ReadAllText("models.json");
            modelsJson.Should().NotBeNullOrEmpty();
            
            var modelNames = JsonSerializer.Deserialize<List<string>>(modelsJson);
            modelNames.Should().NotBeNullOrEmpty();
            modelNames = modelNames.Select(x=> x.Replace("TheBloke/", "")).ToList();

            // Humanize model names and split into tokens
            var humanizedTokens = modelNames.SelectMany(name =>
                name.Humanize(LetterCasing.LowerCase)
                .Split(new[] { '-', '_', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Select(token => new { Token = token, ModelName = name }))
                .GroupBy(x => x.Token)
                .OrderBy(x=> x.Key)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ModelName).Distinct().ToList());

            // Assert that the search index is correctly populated
            humanizedTokens.Should().NotBeNull();
            humanizedTokens.Should().NotBeEmpty();

            var tokens= humanizedTokens.Keys.ToList();
            tokens.Should().NotBeNullOrEmpty();
            
            var tokenString= string.Join(", ", tokens); 


        }
    }
    public class ModelNameMetaDataTests
    {
        [Fact]
        public void Constructor_ParsesFileNameCorrectly()
        {
            // Arrange
            string fileName = "CapybaraHermes-2.5-Mistral-7B-GGUF";

            // Act
            ModelNameMetaData record = new ModelNameMetaData(fileName);

            // Assert
            Assert.Equal("CapybaraHermes", record.ModelName);
            Assert.Equal("2.5", record.Version);
            Assert.Equal("Mistral", record.Configuration);
            Assert.Equal("7B", record.Capacity);
        }

        [Theory]
        [InlineData("CapybaraHermes-2.5-Mistral-7B-GGUF", "CapybaraHermes", "2.5", "Mistral", "7B")]
        [InlineData("KafkaLM-70B-German-V0.1-GGUF", "KafkaLM", "V0.1", "German", "70B")]
        [InlineData("CodeLlama-70B-Instruct-GGUF", "CodeLlama", "", "Instruct", "70B")]
        [InlineData("Tess-34B-v1.5b-GGUF", "Tess", "v1.5b", "", "34B")]
        [InlineData("Goliath-longLORA-120b-rope8-32k-fp16-GGUF", "Goliath", "", "longLORA", "120b")]

        public void ParseModelInfo_ValidFilename_ReturnsCorrectModelInfo(string filename, string expectedModelName, string expectedVersion, string expectedConfiguration, string expectedCapacity)
        {
            var result = new ModelNameMetaData(filename);

            result.ModelName.Should().Be(expectedModelName, because: $"ModelName should match for {filename}");
            result.Version.Should().Be(expectedVersion, because: $"Version should match for {filename}");
            result.Configuration.Should().Be(expectedConfiguration, because: $"Configuration should match for {filename}");
            result.Capacity.Should().Be(expectedCapacity, because: $"Capacity should match for {filename}");
        }
    }
}