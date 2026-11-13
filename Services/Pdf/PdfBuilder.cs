using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Discretion.Services.Pdf;

public enum PdfFont
{
    Regular,
    Bold,
    Italic,
    BoldItalic
}

public readonly record struct PdfColor(float R, float G, float B)
{
    public static readonly PdfColor Black = new(0, 0, 0);
}

/// <summary>
/// Minimal PDF 1.4 writer using the built-in Times font family (no font embedding required).
/// The public drawing API uses a top-left origin with y increasing downward, in points.
/// </summary>
public sealed class PdfBuilder
{
    public const float PageWidth = 612f;  // US Letter
    public const float PageHeight = 792f;

    private static readonly string[] BaseFonts = ["Times-Roman", "Times-Bold", "Times-Italic", "Times-BoldItalic"];
    private readonly List<PdfCanvas> _pages = [];

    public IReadOnlyList<PdfCanvas> Pages => _pages;

    public PdfCanvas AddPage()
    {
        var page = new PdfCanvas();
        _pages.Add(page);
        return page;
    }

    public static float Measure(string text, PdfFont font, float size)
    {
        var widths = WidthsFor(font);
        var total = 0;
        foreach (var c in WinAnsi.Encode(text))
            total += widths[c];
        return total * size / 1000f;
    }

    internal static short[] WidthsFor(PdfFont font) => font switch
    {
        PdfFont.Bold => StandardFontMetrics.TimesBold,
        PdfFont.Italic => StandardFontMetrics.TimesItalic,
        PdfFont.BoldItalic => StandardFontMetrics.TimesBoldItalic,
        _ => StandardFontMetrics.TimesRoman
    };

    public byte[] Build(string title, string author, string subject)
    {
        if (_pages.Count == 0) AddPage();

        // Object numbering: 1 catalog, 2 pages, 3 info, 4..7 fonts, then (page, content) pairs.
        const int fontStart = 4;
        var firstPageObj = fontStart + BaseFonts.Length;
        var objects = new List<byte[]>();

        string Ref(int n) => $"{n} 0 R";

        var kids = string.Join(" ", Enumerable.Range(0, _pages.Count).Select(i => Ref(firstPageObj + i * 2)));
        objects.Add(Ascii($"<< /Type /Catalog /Pages {Ref(2)} >>"));
        objects.Add(Ascii($"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>"));
        var now = DateTimeOffset.Now;
        var offset = now.Offset;
        var date = $"D:{now:yyyyMMddHHmmss}{(offset < TimeSpan.Zero ? '-' : '+')}{Math.Abs(offset.Hours):00}'{Math.Abs(offset.Minutes):00}'";
        objects.Add(Ascii($"<< /Title {PdfString(title)} /Author {PdfString(author)} /Subject {PdfString(subject)} /Producer (Discretion) /Creator (Discretion) /CreationDate ({date}) >>"));

        foreach (var baseFont in BaseFonts)
            objects.Add(Ascii($"<< /Type /Font /Subtype /Type1 /BaseFont /{baseFont} /Encoding /WinAnsiEncoding >>"));

        var fontDict = string.Join(" ", BaseFonts.Select((_, i) => $"/F{i + 1} {Ref(fontStart + i)}"));
        for (var i = 0; i < _pages.Count; i++)
        {
            var contentObj = firstPageObj + i * 2 + 1;
            objects.Add(Ascii($"<< /Type /Page /Parent {Ref(2)} /MediaBox [0 0 {Num(PageWidth)} {Num(PageHeight)}] /Resources << /Font << {fontDict} >> >> /Contents {Ref(contentObj)} >>"));

            var raw = Encoding.Latin1.GetBytes(_pages[i].Content.ToString());
            using var ms = new MemoryStream();
            using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                z.Write(raw);
            var compressed = ms.ToArray();
            using var stream = new MemoryStream();
            stream.Write(Ascii($"<< /Length {compressed.Length} /Filter /FlateDecode >>\nstream\n"));
            stream.Write(compressed);
            stream.Write(Ascii("\nendstream"));
            objects.Add(stream.ToArray());
        }

        using var output = new MemoryStream();
        output.Write(Ascii("%PDF-1.4\n"));
        output.Write([(byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n']);
        var offsets = new long[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = output.Position;
            output.Write(Ascii($"{i + 1} 0 obj\n"));
            output.Write(objects[i]);
            output.Write(Ascii("\nendobj\n"));
        }

        var xref = output.Position;
        var sb = new StringBuilder();
        sb.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets)
            sb.Append($"{o:0000000000} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root {Ref(1)} /Info {Ref(3)} >>\nstartxref\n{xref}\n%%EOF\n");
        output.Write(Ascii(sb.ToString()));
        return output.ToArray();
    }

    internal static string Num(float v) => Math.Round(v, 2).ToString("0.##", CultureInfo.InvariantCulture);

    internal static string PdfString(string text)
    {
        var sb = new StringBuilder("(");
        foreach (var c in WinAnsi.Encode(text))
        {
            switch (c)
            {
                case '(': sb.Append(@"\("); break;
                case ')': sb.Append(@"\)"); break;
                case '\\': sb.Append(@"\\"); break;
                default:
                    if (c < 32) sb.Append(' ');
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append(')').ToString();
    }

    private static byte[] Ascii(string s) => Encoding.Latin1.GetBytes(s);
}

public sealed class PdfCanvas
{
    internal StringBuilder Content { get; } = new();

    private static float Y(float top) => PdfBuilder.PageHeight - top;
    private static string N(float v) => PdfBuilder.Num(v);

    /// <summary>Starts a text object; segments are drawn consecutively from the given baseline origin.</summary>
    public void TextLine(float x, float baseline, IEnumerable<(string Text, PdfFont Font, float Size)> segments,
        float wordSpacing = 0, PdfColor? color = null)
    {
        var c = color ?? PdfColor.Black;
        Content.Append($"BT {N(c.R)} {N(c.G)} {N(c.B)} rg {N(wordSpacing)} Tw {N(x)} {N(Y(baseline))} Td ");
        foreach (var (text, font, size) in segments)
            Content.Append($"/F{(int)font + 1} {N(size)} Tf {PdfBuilder.PdfString(text)} Tj ");
        Content.Append("ET\n");
    }

    public void Text(float x, float baseline, string text, PdfFont font, float size, PdfColor? color = null) =>
        TextLine(x, baseline, [(text, font, size)], 0, color);

    public void TextCentered(float centerX, float baseline, string text, PdfFont font, float size, PdfColor? color = null) =>
        Text(centerX - PdfBuilder.Measure(text, font, size) / 2, baseline, text, font, size, color);

    public void TextRight(float rightX, float baseline, string text, PdfFont font, float size, PdfColor? color = null) =>
        Text(rightX - PdfBuilder.Measure(text, font, size), baseline, text, font, size, color);

    /// <summary>Draws text rotated by the given angle (degrees, counter-clockwise) centered at a point.</summary>
    public void TextRotated(float centerX, float centerY, string text, PdfFont font, float size, float degrees, PdfColor color)
    {
        var rad = degrees * MathF.PI / 180f;
        var cos = MathF.Cos(rad);
        var sin = MathF.Sin(rad);
        var w = PdfBuilder.Measure(text, font, size);
        // Offset so the string's midpoint lands on the requested center.
        var x = centerX - cos * w / 2;
        var y = Y(centerY) - sin * w / 2;
        Content.Append($"BT {N(color.R)} {N(color.G)} {N(color.B)} rg /F{(int)font + 1} {N(size)} Tf {N(cos)} {N(sin)} {N(-sin)} {N(cos)} {N(x)} {N(y)} Tm {PdfBuilder.PdfString(text)} Tj ET\n");
    }

    public void Line(float x1, float y1, float x2, float y2, float width, PdfColor? color = null)
    {
        var c = color ?? PdfColor.Black;
        Content.Append($"{N(c.R)} {N(c.G)} {N(c.B)} RG {N(width)} w {N(x1)} {N(Y(y1))} m {N(x2)} {N(Y(y2))} l S\n");
    }

    public void Rect(float x, float y, float w, float h, float lineWidth, PdfColor stroke, PdfColor? fill = null)
    {
        var op = "S";
        if (fill is { } f)
        {
            Content.Append($"{N(f.R)} {N(f.G)} {N(f.B)} rg ");
            op = "B";
        }
        Content.Append($"{N(stroke.R)} {N(stroke.G)} {N(stroke.B)} RG {N(lineWidth)} w {N(x)} {N(Y(y + h))} {N(w)} {N(h)} re {op}\n");
    }

    /// <summary>Strokes a series of polylines with round caps and joins (used for handwritten signatures).</summary>
    public void Strokes(IEnumerable<IReadOnlyList<(float X, float Y)>> strokes, float width, PdfColor color)
    {
        Content.Append($"q 1 J 1 j {N(width)} w {N(color.R)} {N(color.G)} {N(color.B)} RG\n");
        foreach (var stroke in strokes)
        {
            if (stroke.Count == 0) continue;
            Content.Append($"{N(stroke[0].X)} {N(Y(stroke[0].Y))} m ");
            if (stroke.Count == 1)
                Content.Append($"{N(stroke[0].X + 0.1f)} {N(Y(stroke[0].Y))} l ");
            for (var i = 1; i < stroke.Count; i++)
                Content.Append($"{N(stroke[i].X)} {N(Y(stroke[i].Y))} l ");
            Content.Append("S\n");
        }
        Content.Append("Q\n");
    }
}

/// <summary>Maps Unicode text to single-byte WinAnsiEncoding (Windows-1252) codes, returned as chars 0-255.</summary>
internal static class WinAnsi
{
    private static readonly Dictionary<char, char> Specials = new()
    {
        ['\u20AC'] = (char)0x80, ['\u201A'] = (char)0x82, ['\u0192'] = (char)0x83, ['\u201E'] = (char)0x84,
        ['\u2026'] = (char)0x85, ['\u2020'] = (char)0x86, ['\u2021'] = (char)0x87, ['\u02C6'] = (char)0x88,
        ['\u2030'] = (char)0x89, ['\u0160'] = (char)0x8A, ['\u2039'] = (char)0x8B, ['\u0152'] = (char)0x8C,
        ['\u017D'] = (char)0x8E, ['\u2018'] = (char)0x91, ['\u2019'] = (char)0x92, ['\u201C'] = (char)0x93,
        ['\u201D'] = (char)0x94, ['\u2022'] = (char)0x95, ['\u2013'] = (char)0x96, ['\u2014'] = (char)0x97,
        ['\u02DC'] = (char)0x98, ['\u2122'] = (char)0x99, ['\u0161'] = (char)0x9A, ['\u203A'] = (char)0x9B,
        ['\u0153'] = (char)0x9C, ['\u017E'] = (char)0x9E, ['\u0178'] = (char)0x9F,
    };

    public static string Encode(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is >= ' ' and <= '~' or >= '\u00A0' and <= '\u00FF') sb.Append(ch);
            else if (Specials.TryGetValue(ch, out var mapped)) sb.Append(mapped);
            else if (ch is '\t' or '\n' or '\r') sb.Append(' ');
            else sb.Append('?');
        }
        return sb.ToString();
    }
}
