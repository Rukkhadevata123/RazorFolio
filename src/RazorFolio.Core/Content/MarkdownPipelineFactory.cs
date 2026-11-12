using Markdig;
using Markdig.Extensions.TableOfContents;

namespace RazorFolio.Core.Content;

public static class MarkdownPipelineFactory
{
    public static MarkdownPipeline Create()
    {
        var builder = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UsePipeTables()
            .UseTaskLists()
            .UseFootnotes()
            .UseCustomContainers();
        builder.Extensions.Add(new ExplicitHeadingIdsExtension());

        return builder.UseAutoIdentifiers()
            .UseTableOfContents(new TableOfContentsOptions { MinLevel = 2, MaxLevel = 6 })
            .Build();
    }
}
