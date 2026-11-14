using RazorFolio.Core.Content;
using YamlDotNet.Serialization;

namespace RazorFolio.Core.Serialization;

// The upstream generator supplies the StaticContext base and requires a public declaration.
[YamlStaticContext]
[YamlSerializable(typeof(ArticleMetadata))]
// Preserve the reflection builder's default false when a boolean is written as a mapping.
[YamlSerializable(typeof(bool))]
public partial class RazorFolioYamlContext;
