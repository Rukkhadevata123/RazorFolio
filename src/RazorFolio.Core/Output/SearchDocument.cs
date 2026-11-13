namespace RazorFolio.Core.Output;

// Url is relative to search-index.json, not to the page displaying search results.
// Text is plain author content (including code); consumers must display it as text.
public sealed record SearchDocument(string Title, IReadOnlyList<string> Tags, string Text, string Url);
