using Discretion.Models;
using Discretion.Services;

namespace Discretion.Views;

/// <summary>Modal page that captures one party's handwritten signature.</summary>
public partial class SignaturePage : ContentPage
{
    private readonly TaskCompletionSource<SignatureRecord?> _result = new();
    private readonly Agreement _agreement;
    private readonly DocumentService _documents;
    private bool _completed;

    private SignaturePage(Agreement agreement, string role, string name, DocumentService documents)
    {
        InitializeComponent();
        _agreement = agreement;
        _documents = documents;
        RoleLabel.Text = $"SIGNATURE OF {role.ToUpperInvariant()}";
        NameLabel.Text = name;
        ReferenceLabel.Text = $"Agreement {agreement.ReferenceNumber} · {agreement.TemplateTitle}";
        AttestationLabel.Text =
            $"By signing below, I, {name}, acknowledge that I have read and understood Non-Disclosure Agreement " +
            $"{agreement.ReferenceNumber}, effective {NdaComposer.LongDate(agreement.EffectiveDate)}, and agree to be bound by its terms " +
            $"as the {role}.";
    }

    public static async Task<SignatureRecord?> CaptureAsync(Agreement agreement, string role, string name, DocumentService documents)
    {
        var page = new SignaturePage(agreement, role, name, documents);
        await Shell.Current.Navigation.PushModalAsync(page);
        return await page._result.Task;
    }

    private void UpdateState()
    {
        PlaceholderLabel.IsVisible = !Pad.HasSignature;
        AdoptButton.IsEnabled = Pad.HasSignature && ConsentBox.IsChecked;
    }

    private void OnSignatureChanged(object? sender, EventArgs e) => UpdateState();
    private void OnConsentChanged(object? sender, CheckedChangedEventArgs e) => UpdateState();
    private void OnConsentLabelTapped(object? sender, TappedEventArgs e) => ConsentBox.IsChecked = !ConsentBox.IsChecked;

    private void OnClearClicked(object? sender, EventArgs e)
    {
        Pad.Clear();
        UpdateState();
    }

    private async void OnReviewClicked(object? sender, EventArgs e) =>
        await Navigation.PushModalAsync(new NavigationPage(new DocumentPreviewPage(_agreement, _documents, isModal: true))
        {
            BarBackgroundColor = Color.FromArgb("#0B1F3A"),
            BarTextColor = Colors.White
        });

    private async void OnCancelClicked(object? sender, EventArgs e) => await CompleteAsync(null);

    private async void OnAdoptClicked(object? sender, EventArgs e)
    {
        if (!Pad.HasSignature || !ConsentBox.IsChecked) return;
        await CompleteAsync(Pad.ToRecord());
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CompleteAsync(null);
        return true;
    }

    private async Task CompleteAsync(SignatureRecord? record)
    {
        if (_completed) return;
        _completed = true;
        await Navigation.PopModalAsync();
        _result.TrySetResult(record);
    }
}
