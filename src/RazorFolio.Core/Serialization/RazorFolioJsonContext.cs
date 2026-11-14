using System.Text.Json.Serialization;
using RazorFolio.Core.Configuration;
using RazorFolio.Core.Output;

namespace RazorFolio.Core.Serialization;

// Generate metadata for every persisted JSON shape. Callers keep their own encoding and validation options.
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(SiteConfiguration))]
[JsonSerializable(typeof(SearchIndexWriter.SearchIndex))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(string))]
internal partial class RazorFolioJsonContext : JsonSerializerContext;
