using Markdig;
using Markdig.Extensions.RelativeFileLinks;
using Markdig.Extensions.TableOfContents;

namespace RazorFolio.Core.Content;

public static class MarkdownPipelineFactory
{
    public static MarkdownPipeline Create()
    {
        var fileLinks = new RelativeFileLinkOptions();
        fileLinks.SourceExtensions.Clear();
        fileLinks.SourceExtensions.Add(".md");

        return new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UsePipeTables()
            .UseTaskLists()
            .UseFootnotes()
            .UseCustomContainers()
            .UseAutoIdentifiers()
            .UseTableOfContents(new TableOfContentsOptions { MinLevel = 2, MaxLevel = 6 })
            .UseRelativeFileLinks(fileLinks)
            .Build();
    }
}
