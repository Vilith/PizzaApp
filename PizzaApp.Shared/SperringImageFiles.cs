// Gemensamma storleksgränser och kontroll av bildformatets filsignatur.
namespace PizzaApp.Shared;
public static class SperringImageFiles
{
    public const int MaxBytes = 10 * 1024 * 1024;
    public static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.StartsWith(new byte[] { 255, 216, 255 })) return "image/jpeg";
        if (bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8)) return "image/gif";
        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        if (bytes.Length >= 14 && bytes.StartsWith("BM"u8)) return "image/bmp";
        if (bytes.Length >= 16 && bytes.Slice(4, 4).SequenceEqual("ftyp"u8) &&
            (bytes.Slice(8, 4).SequenceEqual("avif"u8) || bytes.Slice(8, 4).SequenceEqual("avis"u8))) return "image/avif";
        return null;
    }
    public static bool IsValid(string? dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl) || dataUrl.Length > ((MaxBytes + 2) / 3 * 4) + 40) return false;
        var comma = dataUrl.IndexOf(',');
        if (comma < 0) return false;
        try
        {
            var bytes = Convert.FromBase64String(dataUrl[(comma + 1)..]);
            var type = DetectContentType(bytes);
            return bytes.Length <= MaxBytes && type != null && dataUrl[..comma] == $"data:{type};base64";
        }
        catch (FormatException) { return false; }
    }
}
