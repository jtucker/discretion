using Discretion.Models;

namespace Discretion.Controls;

/// <summary>Draws signature strokes. Live mode draws in pad coordinates; fit mode scales a saved signature into the view.</summary>
public sealed class SignatureDrawable : IDrawable
{
    public List<List<PointF>> Strokes { get; } = [];
    public SignatureRecord? Record { get; set; }
    public Color InkColor { get; set; } = Color.FromArgb("#10216B");
    public float StrokeWidth { get; set; } = 2.6f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = InkColor;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        if (Record is { IsEmpty: false } record)
        {
            DrawRecord(canvas, dirtyRect, record);
            return;
        }

        canvas.StrokeSize = StrokeWidth;
        foreach (var stroke in Strokes)
            DrawPolyline(canvas, stroke);
    }

    private void DrawRecord(ICanvas canvas, RectF rect, SignatureRecord record)
    {
        var points = record.Strokes.SelectMany(s => s).ToList();
        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        var bw = Math.Max(points.Max(p => p.X) - minX, 1);
        var bh = Math.Max(points.Max(p => p.Y) - minY, 1);
        const float pad = 4;
        var scale = Math.Min((rect.Width - pad * 2) / bw, (rect.Height - pad * 2) / bh);
        var offX = rect.X + pad;
        var offY = rect.Y + pad + (rect.Height - pad * 2 - bh * scale);

        canvas.StrokeSize = Math.Clamp(StrokeWidth * scale, 1.2f, StrokeWidth);
        foreach (var stroke in record.Strokes)
            DrawPolyline(canvas, stroke.Select(p => new PointF(offX + (p.X - minX) * scale, offY + (p.Y - minY) * scale)).ToList());
    }

    private static void DrawPolyline(ICanvas canvas, IReadOnlyList<PointF> stroke)
    {
        if (stroke.Count == 0) return;
        if (stroke.Count == 1)
        {
            canvas.DrawLine(stroke[0], new PointF(stroke[0].X + 0.5f, stroke[0].Y));
            return;
        }
        var path = new PathF(stroke[0]);
        for (var i = 1; i < stroke.Count; i++)
            path.LineTo(stroke[i]);
        canvas.DrawPath(path);
    }
}
