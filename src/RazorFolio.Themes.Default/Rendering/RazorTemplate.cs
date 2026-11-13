using System.Text;
using System.Text.Encodings.Web;

namespace RazorFolio.Themes.Default.Rendering;

internal abstract class RazorTemplate
{
    private readonly StringBuilder output = new();
    private AttributeOutput? attribute;
    private CancellationToken cancellationToken;
    private int started;

    protected abstract Task ExecuteAsync();

    public async Task<HtmlContent> RenderAsync(CancellationToken token = default)
    {
        if (Interlocked.Exchange(ref started, 1) != 0)
            throw new InvalidOperationException("Create a new template instance for each render.");

        cancellationToken = token;
        token.ThrowIfCancellationRequested();
        await ExecuteAsync();
        token.ThrowIfCancellationRequested();
        return new HtmlContent(output.ToString());
    }

    protected static HtmlContent Raw(string value) => new(value);
    protected Task<HtmlContent> PartialAsync(RazorTemplate template) => template.RenderAsync(cancellationToken);
    protected void WriteLiteral(string? value) => output.Append(value);
    protected void Write(object? value) => AppendValue(output, value, encode: true);

    // Razor-generated calls include source offsets for instrumentation; this renderer only writes HTML.
    protected void BeginWriteAttribute(string name, string prefix, int prefixOffset,
        string suffix, int suffixOffset, int valueCount)
    {
        if (attribute is not null) throw new InvalidOperationException("An attribute is already open.");
        attribute = new AttributeOutput(name, prefix, suffix, valueCount);
    }

    protected void WriteAttributeValue(string prefix, int prefixOffset, object? value,
        int valueOffset, int valueLength, bool isLiteral)
    {
        var current = attribute ?? throw new InvalidOperationException("No attribute is open.");
        if (current.ValueCount == 1 && prefix.Length == 0)
        {
            if (value is null or false)
            {
                current.Suppressed = true;
                return;
            }
            if (value is true) value = current.Name;
        }
        if (value is null) return;

        current.Content.Append(prefix);
        AppendValue(current.Content, value, encode: !isLiteral);
    }

    protected void EndWriteAttribute()
    {
        var current = attribute ?? throw new InvalidOperationException("No attribute is open.");
        if (!current.Suppressed)
            output.Append(current.Prefix).Append(current.Content).Append(current.Suffix);
        attribute = null;
    }

    private static void AppendValue(StringBuilder target, object? value, bool encode)
    {
        if (value is HtmlContent html)
        {
            target.Append(html.Value);
            return;
        }

        var text = value?.ToString();
        if (text is not null) target.Append(encode ? HtmlEncoder.Default.Encode(text) : text);
    }

    private sealed class AttributeOutput(string name, string prefix, string suffix, int valueCount)
    {
        public string Name { get; } = name;
        public string Prefix { get; } = prefix;
        public string Suffix { get; } = suffix;
        public int ValueCount { get; } = valueCount;
        public bool Suppressed { get; set; }
        public StringBuilder Content { get; } = new();
    }
}

internal abstract class RazorTemplate<TModel> : RazorTemplate
{
    public required TModel Model { get; init; }
}
