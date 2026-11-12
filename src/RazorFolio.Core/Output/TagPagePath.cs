using System.Security.Cryptography;
using System.Text;

namespace RazorFolio.Core.Output;

public static class TagPagePath
{
    public static string GetOutputPath(string outputDirectory, string name)
    {
        // Bound file-name length and preserve case-sensitive tag identity on every filesystem.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        return Path.Combine(outputDirectory, "tags", Convert.ToHexStringLower(hash) + ".html");
    }
}
