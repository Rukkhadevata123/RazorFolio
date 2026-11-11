using Markdig;

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
            .Build();
    }
}
