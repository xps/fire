using System.Text;

namespace Fire.Core;

/// <summary>
/// Reads a file preserving its encoding: BOMs (UTF-8/16/32) are detected and kept,
/// BOM-less files are decoded as strict UTF-8 with a Latin-1 fallback (which
/// round-trips every byte). Files containing NUL bytes and no BOM are flagged
/// binary so callers can skip them.
/// </summary>
public sealed class TextFile
{
    public string Content { get; private set; } = "";
    public bool IsBinary { get; private set; }

    private Encoding encoding = new UTF8Encoding(false);
    private byte[] preamble = [];

    private TextFile() { }

    public static TextFile Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var file = new TextFile();

        var bom = DetectBom(bytes);
        if (bom != null)
        {
            (file.encoding, file.preamble) = bom.Value;
            file.Content = file.encoding.GetString(bytes, file.preamble.Length, bytes.Length - file.preamble.Length);
            return file;
        }

        if (LooksBinary(bytes))
        {
            file.IsBinary = true;
            return file;
        }

        try
        {
            file.Content = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            file.encoding = Encoding.Latin1;
            file.Content = file.encoding.GetString(bytes);
        }

        return file;
    }

    public void Write(string path, string newContent)
    {
        using var stream = File.Create(path);
        stream.Write(preamble);
        stream.Write(encoding.GetBytes(newContent));
    }

    private static (Encoding Encoding, byte[] Preamble)? DetectBom(byte[] b)
    {
        // UTF-32 LE must be tested before UTF-16 LE (same first two bytes).
        if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00)
            return (new UTF32Encoding(bigEndian: false, byteOrderMark: false), b[..4]);
        if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF)
            return (new UTF32Encoding(bigEndian: true, byteOrderMark: false), b[..4]);
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            return (new UTF8Encoding(false), b[..3]);
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            return (new UnicodeEncoding(bigEndian: false, byteOrderMark: false), b[..2]);
        if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            return (new UnicodeEncoding(bigEndian: true, byteOrderMark: false), b[..2]);
        return null;
    }

    private static bool LooksBinary(byte[] bytes)
    {
        var length = Math.Min(bytes.Length, 8000);
        for (var i = 0; i < length; i++)
            if (bytes[i] == 0)
                return true;
        return false;
    }
}
