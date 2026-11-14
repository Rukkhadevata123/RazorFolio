using System.Security.Cryptography;

namespace RazorFolio.Core.Assets;

internal static class ContentFingerprint
{
    public static string AddToPath(string path, byte[] content)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(content))[..16];
        return Path.Combine(Path.GetDirectoryName(path) ?? "",
            $"{Path.GetFileNameWithoutExtension(path)}.{hash}{Path.GetExtension(path)}");
    }
}
