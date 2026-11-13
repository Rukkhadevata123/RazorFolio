namespace RazorFolio.Core.Diagnostics;

public sealed record ContentLinkDiagnostic(
    string SourcePath, int Line, int Column, string Code, string Link, string Message);
