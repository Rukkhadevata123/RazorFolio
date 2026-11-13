using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: RazorFolio.TemplateCompiler <views-directory> <output-directory>");
    return 2;
}

var viewsDirectory = Path.GetFullPath(args[0]);
var outputDirectory = Path.GetFullPath(args[1]);
if (!Directory.Exists(viewsDirectory)) throw new DirectoryNotFoundException(viewsDirectory);
if (outputDirectory == viewsDirectory) throw new ArgumentException("Generated files need a separate directory.");

var engine = RazorProjectEngine.Create(RazorConfiguration.Default,
    RazorProjectFileSystem.Create(viewsDirectory), builder =>
    {
        builder.Features.Add(new TemplateClassifier());
        InheritsDirective.Register(builder);
        builder.ConfigureCodeGenerationOptions(options => options.SuppressMetadataAttributes = true);
    });

var generated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var hasErrors = false;
foreach (var sourcePath in Directory.EnumerateFiles(viewsDirectory, "*.cshtml", SearchOption.AllDirectories)
    .Order(StringComparer.Ordinal))
{
    var relativePath = Path.GetRelativePath(viewsDirectory, sourcePath).Replace('\\', '/');
    var className = Path.ChangeExtension(relativePath, null).Replace('/', '_');
    if (className.Length == 0 || !(char.IsAsciiLetter(className[0]) || className[0] == '_')
        || className.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '_')))
        throw new InvalidDataException($"Use identifier characters in template paths: {relativePath}");

    var result = engine.Process(engine.FileSystem.GetItem("/" + relativePath)).GetCSharpDocument();
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic);
        hasErrors |= diagnostic.Severity == RazorDiagnosticSeverity.Error;
    }
    if (!generated.TryAdd("RazorFolioTemplate_" + className + ".g.cs", result.GeneratedCode))
        throw new InvalidDataException($"Template class name collision: {relativePath}");
}
if (hasErrors) return 1;

Directory.CreateDirectory(outputDirectory);
foreach (var (name, code) in generated)
{
    var path = Path.Combine(outputDirectory, name);
    if (!File.Exists(path) || File.ReadAllText(path) != code)
        File.WriteAllText(path, code, new UTF8Encoding(false));
}
// Only remove this compiler's generated files, including templates deleted since the previous build.
foreach (var path in Directory.EnumerateFiles(outputDirectory, "RazorFolioTemplate_*.g.cs"))
{
    if (!generated.ContainsKey(Path.GetFileName(path))) File.Delete(path);
}
return 0;

sealed class TemplateClassifier : DocumentClassifierPassBase
{
    public override int Order => -100;
    protected override string DocumentKind => "razorfolio.template";
    protected override bool IsMatch(RazorCodeDocument document, DocumentIntermediateNode node) => true;

    protected override void OnDocumentStructureCreated(RazorCodeDocument document,
        NamespaceDeclarationIntermediateNode ns, ClassDeclarationIntermediateNode type,
        MethodDeclarationIntermediateNode method)
    {
        ns.Name = "RazorFolio.Themes.Default.Generated";
        var relativePath = document.Source.RelativePath!.Replace('\\', '/').TrimStart('/');
        type.Name = Path.ChangeExtension(relativePath, null).Replace('/', '_');
        type.Modifiers = ["internal", "sealed"];
        method.Name = "ExecuteAsync";
        method.ReturnType = "global::System.Threading.Tasks.Task";
        method.Modifiers = ["protected", "override", "async"];
    }
}
