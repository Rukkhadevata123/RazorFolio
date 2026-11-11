using Markdig;

namespace RazorFolio.Core.Rendering;

public sealed class MarkdownRenderer
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder().Build();

    public string Render(string markdown)
    {
        return Markdown.ToHtml(markdown, pipeline);
    }
}
