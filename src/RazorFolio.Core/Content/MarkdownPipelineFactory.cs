using Markdig;
using Markdig.Extensions.TableOfContents;

namespace RazorFolio.Core.Content;

public static class MarkdownPipelineFactory
{
    public static MarkdownPipeline Create()
    {
        return new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UsePipeTables()
            .UseTaskLists()
            .UseFootnotes()
            .UseCustomContainers()
            .UseAutoIdentifiers()
            .UseTableOfContents(new TableOfContentsOptions { MinLevel = 2, MaxLevel = 6 })
            .Build();
    }
}
