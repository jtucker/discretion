using Discretion.Models;

namespace Discretion.Controls;

/// <summary>A touch/mouse signature capture surface.</summary>
public sealed class SignaturePad : GraphicsView
{
    private readonly SignatureDrawable _drawable = new();
    private List<PointF>? _current;

    public static readonly BindableProperty HasSignatureProperty =
        BindableProperty.Create(nameof(HasSignature), typeof(bool), typeof(SignaturePad), false, BindingMode.OneWayToSource);

    public bool HasSignature
    {
        get => (bool)GetValue(HasSignatureProperty);
        private set => SetValue(HasSignatureProperty, value);
    }

    public event EventHandler? SignatureChanged;

    public SignaturePad()
    {
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        StartInteraction += OnStart;
        DragInteraction += OnDrag;
        EndInteraction += OnEnd;
        CancelInteraction += (_, _) => _current = null;
    }

    public void Clear()
    {
        _drawable.Strokes.Clear();
        _current = null;
        Invalidate();
        Update();
    }

    public SignatureRecord ToRecord() => new()
    {
        CanvasWidth = (float)Width,
        CanvasHeight = (float)Height,
        Strokes = _drawable.Strokes
            .Select(s => s.Select(p => new SignaturePoint { X = p.X, Y = p.Y }).ToList())
            .ToList()
    };

    private void OnStart(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length == 0) return;
        _current = [e.Touches[0]];
        _drawable.Strokes.Add(_current);
        Invalidate();
    }

    private void OnDrag(object? sender, TouchEventArgs e)
    {
        if (_current is null || e.Touches.Length == 0) return;
        var p = e.Touches[0];
        var last = _current[^1];
        // Drop sub-pixel jitter to keep stored signatures compact.
        if (MathF.Abs(p.X - last.X) + MathF.Abs(p.Y - last.Y) < 1.2f) return;
        _current.Add(p);
        Invalidate();
    }

    private void OnEnd(object? sender, TouchEventArgs e)
    {
        if (_current is not null && e.Touches.Length > 0)
            _current.Add(e.Touches[0]);
        _current = null;
        Invalidate();
        Update();
    }

    private void Update()
    {
        HasSignature = _drawable.Strokes.Sum(s => s.Count) >= 6;
        SignatureChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Read-only view of a saved signature.</summary>
public sealed class SignatureView : GraphicsView
{
    private readonly SignatureDrawable _drawable = new() { StrokeWidth = 2.2f };

    public static readonly BindableProperty SignatureProperty =
        BindableProperty.Create(nameof(Signature), typeof(SignatureRecord), typeof(SignatureView), null,
            propertyChanged: (b, _, n) =>
            {
                var view = (SignatureView)b;
                view._drawable.Record = n as SignatureRecord;
                view.Invalidate();
            });

    public SignatureRecord? Signature
    {
        get => (SignatureRecord?)GetValue(SignatureProperty);
        set => SetValue(SignatureProperty, value);
    }

    public SignatureView()
    {
        Drawable = _drawable;
        BackgroundColor = Colors.Transparent;
        InputTransparent = true;
    }
}
