using Discretion.Controls;
using Discretion.Models;
using Discretion.Services;

namespace Discretion.Views;

/// <summary>Renders the agreement natively as a paper-style document, from the same model used for the PDF.</summary>
public sealed class DocumentPreviewPage : ContentPage
{
    private readonly Agreement _agreement;
    private readonly DocumentService _documents;

    private static readonly Color Navy = Color.FromArgb("#0B1F3A");
    private static readonly Color Gold = Color.FromArgb("#B08D57");
    private static readonly Color Muted = Color.FromArgb("#646B78");
    private static readonly Color Ink = Color.FromArgb("#1A1D24");

    public DocumentPreviewPage(Agreement agreement, DocumentService documents, bool isModal = false)
    {
        _agreement = agreement;
        _documents = documents;
        Title = agreement.ReferenceNumber;
        BackgroundColor = Color.FromArgb("#E9E5DC");

        if (isModal)
            ToolbarItems.Add(new ToolbarItem("Close", null, async () => await Navigation.PopModalAsync()));
        else
            ToolbarItems.Add(new ToolbarItem("Share PDF", null, async () => await ShareAsync()));

        Content = new ScrollView { Content = BuildPaper() };
    }

    private static string Serif => (string)Application.Current!.Resources["SerifFont"];

    private View BuildPaper()
    {
        var doc = NdaComposer.Compose(_agreement);
        var stack = new VerticalStackLayout { Spacing = 10 };

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case TitleBlock t:
                    stack.Add(Centered("STRICTLY PRIVATE & CONFIDENTIAL", 9, Gold, FontAttributes.Bold));
                    stack.Add(Centered(t.Title, 20, Navy, FontAttributes.Bold, margin: new Thickness(0, 8, 0, 0)));
                    stack.Add(Centered($"Re: {t.Subtitle}", 15, Navy, FontAttributes.Italic));
                    stack.Add(Centered($"Agreement No. {t.Reference}", 11, Muted));
                    stack.Add(new BoxView { HeightRequest = 1.5, Color = Gold, BackgroundColor = Colors.Transparent, Margin = new Thickness(0, 6, 0, 0) });
                    stack.Add(new BoxView { HeightRequest = 0.5, Color = Gold, BackgroundColor = Colors.Transparent, Margin = new Thickness(0, -8, 0, 8) });
                    break;
                case SectionHeadingBlock h:
                    stack.Add(Centered(h.Text, 13, Navy, FontAttributes.Bold, margin: new Thickness(0, 8, 0, 0)));
                    break;
                case ParagraphBlock p:
                    stack.Add(Paragraph(p));
                    break;
                case SignatureBlock s:
                    stack.Add(Signature(s));
                    break;
            }
        }

        stack.Add(new Label
        {
            Text = $"{doc.ReferenceNumber} · Doc. ID {doc.Fingerprint}",
            FontSize = 10,
            TextColor = Muted,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        });

        var paper = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#D6D0C2"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
            Padding = new Thickness(22, 26),
            Margin = new Thickness(12, 16, 12, 28),
            MaximumWidthRequest = 760,
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.12f, Radius = 10, Offset = new Point(0, 3) },
            Content = stack
        };

        if (!_agreement.IsFullyExecuted)
        {
            return new Grid
            {
                Children =
                {
                    paper,
                    new Label
                    {
                        Text = "NOT EXECUTED",
                        FontSize = 46,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#14B08D57"),
                        Rotation = -50,
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(0, 280, 0, 0),
                        InputTransparent = true
                    }
                }
            };
        }
        return paper;
    }

    private static Label Centered(string text, double size, Color color, FontAttributes attrs = FontAttributes.None, Thickness? margin = null) => new()
    {
        Text = text,
        FontFamily = Serif,
        FontSize = size,
        TextColor = color,
        FontAttributes = attrs,
        HorizontalTextAlignment = TextAlignment.Center,
        Margin = margin ?? Thickness.Zero
    };

    private static Label Paragraph(ParagraphBlock p)
    {
        var formatted = new FormattedString();
        foreach (var run in p.Runs)
        {
            formatted.Spans.Add(new Span
            {
                Text = run.Text,
                FontFamily = Serif,
                FontSize = 14,
                TextColor = Ink,
                FontAttributes = run.Style switch
                {
                    RunStyle.Bold => FontAttributes.Bold,
                    RunStyle.Italic => FontAttributes.Italic,
                    _ => FontAttributes.None
                }
            });
        }
        return new Label
        {
            FormattedText = formatted,
            LineHeight = 1.3,
            HorizontalTextAlignment = p.Centered ? TextAlignment.Center : TextAlignment.Justify,
            Margin = new Thickness(p.Indent, 0, 0, 0)
        };
    }

    private static View Signature(SignatureBlock s)
    {
        var stack = new VerticalStackLayout { Spacing = 3, Margin = new Thickness(0, 18, 0, 0) };
        stack.Add(new Label { Text = s.Role, FontFamily = Serif, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Navy });

        var area = new Grid { HeightRequest = 72 };
        if (s.Signature is { IsEmpty: false })
            area.Add(new SignatureView { Signature = s.Signature, HeightRequest = 64, VerticalOptions = LayoutOptions.End });
        else
            area.Add(new Label { Text = "X", FontFamily = Serif, FontSize = 20, TextColor = Muted, VerticalOptions = LayoutOptions.End, Margin = new Thickness(2, 0, 0, 4) });
        area.Add(new BoxView { HeightRequest = 1, Color = Navy, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.End });
        stack.Add(area);

        stack.Add(new Label { Text = "Signature", FontFamily = Serif, FontSize = 10, FontAttributes = FontAttributes.Italic, TextColor = Muted });
        stack.Add(Detail("Name", s.Party.Name));
        stack.Add(Detail("Date", s.Signature is { } sig ? NdaComposer.LongDate(sig.SignedAt) : "______________________"));
        if (s.Signature is { } audit)
        {
            stack.Add(new Label
            {
                Text = $"Electronically signed {audit.SignedAt:MMM d, yyyy h:mm tt} · Doc. ID {audit.DocumentFingerprint} · {audit.Device}",
                FontSize = 10,
                FontAttributes = FontAttributes.Italic,
                TextColor = Muted
            });
        }
        return stack;
    }

    private static Label Detail(string label, string value) => new()
    {
        FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = $"{label}:  ", FontFamily = Serif, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Ink },
                new Span { Text = value, FontFamily = Serif, FontSize = 13, TextColor = Ink }
            }
        }
    };

    private async Task ShareAsync()
    {
        var path = await _documents.GetShareableCopyAsync(_agreement);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Non-Disclosure Agreement {_agreement.ReferenceNumber}",
            File = new ShareFile(path, "application/pdf")
        });
    }
}
