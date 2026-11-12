namespace RazorFolio.Core.Rendering;

public sealed record Pagination(int CurrentPage, int TotalPages, string? PreviousUrl, string? NextUrl);
