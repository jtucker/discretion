using System.Globalization;
using Discretion.Models;

namespace Discretion.Services.Pdf;

/// <summary>Lays out an <see cref="AgreementDocument"/> onto US Letter pages.</summary>
public sealed class AgreementPdfRenderer
{
    private const float Margin = 72f;
    private const float TopMargin = 66f;
    private const float BottomLimit = PdfBuilder.PageHeight - 78f;
    private const float ContentWidth = PdfBuilder.PageWidth - Margin * 2;
    private const float BodySize = 10.5f;
    private const float Leading = 14.2f;
    private const float ParagraphGap = 7f;

    private static readonly PdfColor Navy = new(0.043f, 0.122f, 0.227f);
    private static readonly PdfColor Gold = new(0.69f, 0.553f, 0.341f);
    private static readonly PdfColor Gray = new(0.42f, 0.42f, 0.45f);
    private static readonly PdfColor Ink = new(0.06f, 0.13f, 0.42f);
    private static readonly PdfColor Watermark = new(0.95f, 0.94f, 0.92f);

    private readonly PdfBuilder _pdf = new();
    private PdfCanvas _page = null!;
    private float _y;
    private bool _draft;

    public static byte[] Render(AgreementDocument doc, Agreement agreement)
    {
        var r = new AgreementPdfRenderer { _draft = !agreement.IsFullyExecuted };
        return r.RenderInternal(doc, agreement);
    }

    private byte[] RenderInternal(AgreementDocument doc, Agreement agreement)
    {
        NewPage();
        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case TitleBlock t: DrawTitle(t); break;
                case SectionHeadingBlock h: DrawHeading(h.Text); break;
                case ParagraphBlock p: DrawParagraph(p); break;
            }
        }

        var signatures = doc.Blocks.OfType<SignatureBlock>().ToList();
        if (signatures.Count > 0) DrawSignatures(signatures, doc.Fingerprint);

        DrawFurniture(doc);
        return _pdf.Build(
            $"Non-Disclosure Agreement {doc.ReferenceNumber}",
            agreement.DisclosingParty.Name,
            agreement.TemplateTitle);
    }

    private void NewPage()
    {
        _page = _pdf.AddPage();
        if (_draft)
            _page.TextRotated(PdfBuilder.PageWidth / 2, PdfBuilder.PageHeight / 2, "NOT EXECUTED", PdfFont.Bold, 72, 50, Watermark);
        _y = _pdf.Pages.Count == 1 ? TopMargin : TopMargin + 22;
    }

    private void EnsureSpace(float height)
    {
        if (_y + height > BottomLimit) NewPage();
    }

    private void DrawTitle(TitleBlock t)
    {
        var cx = PdfBuilder.PageWidth / 2;
        _page.TextCentered(cx, _y + 8, "STRICTLY PRIVATE & CONFIDENTIAL", PdfFont.Bold, 7.5f, Gold);
        _y += 34;
        _page.TextCentered(cx, _y, t.Title, PdfFont.Bold, 18, Navy);
        _y += 20;
        _page.TextCentered(cx, _y, $"Re: {t.Subtitle}", PdfFont.Italic, 12, Navy);
        _y += 16;
        _page.TextCentered(cx, _y, $"Agreement No. {t.Reference}", PdfFont.Regular, 9, Gray);
        _y += 12;
        _page.Line(Margin, _y, PdfBuilder.PageWidth - Margin, _y, 1.2f, Gold);
        _page.Line(Margin, _y + 2.5f, PdfBuilder.PageWidth - Margin, _y + 2.5f, 0.4f, Gold);
        _y += 22;
    }

    private void DrawHeading(string text)
    {
        EnsureSpace(Leading * 4);
        _y += 6;
        _page.TextCentered(PdfBuilder.PageWidth / 2, _y + 9, text, PdfFont.Bold, 11, Navy);
        _y += 22;
    }

    private readonly record struct Word(string Text, PdfFont Font, int TrailingSpaces);

    private void DrawParagraph(ParagraphBlock p)
    {
        var words = Tokenize(p.Runs);
        var left = Margin + p.Indent;
        var width = ContentWidth - p.Indent;
        var lines = WrapLines(words, width);

        // Avoid stranding a single line at the bottom of a page.
        EnsureSpace(Math.Min(lines.Count, 2) * Leading);
        for (var i = 0; i < lines.Count; i++)
        {
            EnsureSpace(Leading);
            var line = lines[i];
            var isLast = i == lines.Count - 1;
            var natural = LineWidth(line);
            var spaceCount = line.Take(line.Count - 1).Sum(w => w.TrailingSpaces);
            var wordSpacing = !isLast && !p.Centered && spaceCount > 0 ? (width - natural) / spaceCount : 0;
            var x = p.Centered ? left + (width - natural) / 2 : left;

            var segments = line.Select((w, idx) =>
                (idx == line.Count - 1 ? w.Text : w.Text + new string(' ', w.TrailingSpaces), w.Font, BodySize));
            _page.TextLine(x, _y + BodySize, MergeSegments(segments), wordSpacing);
            _y += Leading;
        }
        _y += ParagraphGap;
    }

    private static IEnumerable<(string, PdfFont, float)> MergeSegments(IEnumerable<(string Text, PdfFont Font, float Size)> segments)
    {
        string? text = null;
        PdfFont font = default;
        float size = 0;
        foreach (var s in segments)
        {
            if (text is not null && s.Font == font)
            {
                text += s.Text;
                continue;
            }
            if (text is not null) yield return (text, font, size);
            (text, font, size) = s;
        }
        if (text is not null) yield return (text, font, size);
    }

    private static List<Word> Tokenize(IEnumerable<TextRun> runs)
    {
        var words = new List<Word>();
        foreach (var run in runs)
        {
            var font = run.Style switch
            {
                RunStyle.Bold => PdfFont.Bold,
                RunStyle.Italic => PdfFont.Italic,
                _ => PdfFont.Regular
            };
            var text = run.Text;
            var i = 0;
            // Leading spaces attach to the previous word.
            while (i < text.Length && text[i] == ' ')
            {
                if (words.Count > 0) words[^1] = words[^1] with { TrailingSpaces = words[^1].TrailingSpaces + 1 };
                i++;
            }
            while (i < text.Length)
            {
                var start = i;
                while (i < text.Length && text[i] != ' ') i++;
                var word = text[start..i];
                var spaces = 0;
                while (i < text.Length && text[i] == ' ') { spaces++; i++; }
                words.Add(new Word(word, font, spaces));
            }
        }
        return words;
    }

    private static float WordWidth(Word w) => PdfBuilder.Measure(w.Text, w.Font, BodySize);
    private static float SpaceWidth(Word w) => PdfBuilder.Measure(" ", w.Font, BodySize) * w.TrailingSpaces;

    private static float LineWidth(List<Word> line)
    {
        var total = 0f;
        for (var i = 0; i < line.Count; i++)
        {
            total += WordWidth(line[i]);
            if (i < line.Count - 1) total += SpaceWidth(line[i]);
        }
        return total;
    }

    private static List<List<Word>> WrapLines(List<Word> words, float maxWidth)
    {
        var lines = new List<List<Word>>();
        var current = new List<Word>();
        var width = 0f;
        foreach (var word in words)
        {
            var ww = WordWidth(word);
            var gap = current.Count > 0 ? SpaceWidth(current[^1]) : 0;
            if (current.Count > 0 && width + gap + ww > maxWidth)
            {
                lines.Add(current);
                current = [];
                width = 0;
                gap = 0;
            }
            current.Add(word);
            width += gap + ww;
        }
        if (current.Count > 0) lines.Add(current);
        return lines;
    }

    private void DrawSignatures(List<SignatureBlock> signatures, string fingerprint)
    {
        const float blockHeight = 170f;
        const float gap = 36f;
        var colWidth = (ContentWidth - gap) / 2;
        EnsureSpace(blockHeight + 10);
        _y += 8;
        var top = _y;

        for (var i = 0; i < signatures.Count; i++)
        {
            var s = signatures[i];
            var x = Margin + (i % 2) * (colWidth + gap);
            if (i > 0 && i % 2 == 0)
            {
                top += blockHeight;
                if (top + blockHeight > BottomLimit) { NewPage(); top = _y; }
            }
            DrawSignatureBlock(s, x, top, colWidth, fingerprint);
        }
        _y = top + blockHeight;
    }

    private void DrawSignatureBlock(SignatureBlock s, float x, float top, float width, string fingerprint)
    {
        var us = CultureInfo.GetCultureInfo("en-US");
        _page.Text(x, top + 10, s.Role, PdfFont.Bold, 9.5f, Navy);

        var lineY = top + 70;
        if (s.Signature is { IsEmpty: false } sig)
            DrawInk(sig, x + 4, top + 18, width - 8, lineY - top - 20);
        else
            _page.Text(x + 2, lineY - 6, "X", PdfFont.Bold, 14, Gray);

        _page.Line(x, lineY, x + width, lineY, 0.7f, Navy);
        _page.Text(x, lineY + 10, "Signature", PdfFont.Italic, 7.5f, Gray);

        var y = lineY + 26;
        void Row(string label, string value)
        {
            _page.TextLine(x, y, [(label, PdfFont.Bold, 8.5f), ("  " + value, PdfFont.Regular, 8.5f)]);
            y += 12;
        }

        Row("Name:", s.Party.Name);
        Row("Date:", s.Signature is { } signed ? signed.SignedAt.ToString("MMMM d, yyyy", us) : "______________________");
        if (!string.IsNullOrWhiteSpace(s.Party.Email)) Row("Email:", s.Party.Email);
        if (!string.IsNullOrWhiteSpace(s.Party.Phone)) Row("Phone:", s.Party.Phone);

        if (s.Signature is { } audit)
        {
            y += 2;
            var note = $"Electronically signed {audit.SignedAt.ToString("MMM d, yyyy h:mm tt", us)}";
            _page.Text(x, y, note, PdfFont.Italic, 7, Gray);
            _page.Text(x, y + 9, $"Doc. ID {audit.DocumentFingerprint} \u2022 {audit.Device}", PdfFont.Italic, 7, Gray);
        }
    }

    private void DrawInk(SignatureRecord sig, float x, float y, float w, float h)
    {
        var points = sig.Strokes.SelectMany(s => s).ToList();
        if (points.Count == 0) return;
        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);
        var bw = Math.Max(maxX - minX, 1);
        var bh = Math.Max(maxY - minY, 1);
        var scale = Math.Min(w / bw, h / bh);
        // Sit the signature on the baseline, left-aligned.
        var offX = x;
        var offY = y + (h - bh * scale);
        var strokes = sig.Strokes
            .Select(s => (IReadOnlyList<(float, float)>)s.Select(p => (offX + (p.X - minX) * scale, offY + (p.Y - minY) * scale)).ToList());
        _page.Strokes(strokes, 1.3f, Ink);
    }

    private void DrawFurniture(AgreementDocument doc)
    {
        var total = _pdf.Pages.Count;
        var right = PdfBuilder.PageWidth - Margin;
        for (var i = 0; i < total; i++)
        {
            var page = _pdf.Pages[i];
            if (i > 0)
            {
                page.Text(Margin, 50, $"Non-Disclosure Agreement \u2022 {doc.ReferenceNumber}", PdfFont.Italic, 8, Gray);
                page.TextRight(right, 50, "CONFIDENTIAL", PdfFont.Bold, 8, Gold);
                page.Line(Margin, 56, right, 56, 0.4f, Gold);
            }

            var fy = PdfBuilder.PageHeight - 46;
            page.Line(Margin, fy - 12, right, fy - 12, 0.4f, Gold);
            page.Text(Margin, fy, $"{doc.ReferenceNumber}  \u2022  Doc. ID {doc.Fingerprint}", PdfFont.Regular, 7.5f, Gray);
            page.TextCentered(PdfBuilder.PageWidth / 2, fy, $"Page {i + 1} of {total}", PdfFont.Regular, 7.5f, Gray);
            page.TextRight(right, fy, "Initials: ______  ______", PdfFont.Regular, 7.5f, Gray);
        }
    }
}
