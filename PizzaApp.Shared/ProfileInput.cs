using System.ComponentModel.DataAnnotations;
using System.Buffers.Binary;

namespace PizzaApp.Shared;

public class ProfileInput
{
    [Required(ErrorMessage = "Ange ditt namn eller nick.")]
    [StringLength(100, ErrorMessage = "Namnet får innehålla högst 100 tecken.")]
    public string DisplayName { get; set; } = "";
    [StringLength(ProfileImages.MaxDataUrlLength)]
    public string? AvatarDataUrl { get; set; }
    public Guid Revision { get; set; }
}

public static class ProfileImages
{
    public const int MaxBytes = 256 * 1024;
    public const int MaxDataUrlLength = 350000;
    public const string Prefix = "data:image/png;base64,";

    // Only bounded PNG thumbnails are accepted, never external URLs or SVG.
    public static bool IsValid(string? value)
    {
        if (value == null) return true;
        if (value.Length > MaxDataUrlLength || !value.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        byte[] bytes;
        try { bytes = Convert.FromBase64String(value[Prefix.Length..]); }
        catch (FormatException) { return false; }
        if (bytes.Length < 45 || bytes.Length > MaxBytes
            || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)) != 13
            || !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return false;
        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width is 0 or > 256 || height is 0 or > 256) return false;
        var offset = 8;
        var hasImageData = false;
        while (offset <= bytes.Length - 12)
        {
            var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > bytes.Length - offset - 12) return false;
            var kind = bytes.AsSpan(offset + 4, 4);
            if (kind.SequenceEqual("IDAT"u8) && length > 0) hasImageData = true;
            if (kind.SequenceEqual("IEND"u8)) return length == 0 && offset + 12 == bytes.Length && hasImageData;
            offset += (int)length + 12;
        }
        return false;
    }
}
