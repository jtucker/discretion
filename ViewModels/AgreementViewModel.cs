using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discretion.Models;
using Discretion.Services;
using Discretion.Views;

namespace Discretion.ViewModels;

public sealed partial class AgreementViewModel(AgreementStore store, DocumentService documents)
    : ObservableObject, IQueryAttributable
{
    private bool _promptPending;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EffectiveText), nameof(ExpirationText), nameof(GoverningLawText), nameof(SurvivalText))]
    public partial Agreement? Agreement { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public string EffectiveText => Agreement is null ? "" : NdaComposer.LongDate(Agreement.EffectiveDate);
    public string ExpirationText => Agreement is null ? "" : NdaComposer.LongDate(Agreement.ExpirationDate);
    public string GoverningLawText => Agreement is null ? "" : UsStates.Jurisdiction(Agreement.GoverningState);
    public string SurvivalText => Agreement?.SurvivesExpiration == true ? "Survives expiration" : "Ends at expiration";

    public string DisclosingStatus => SignedText(Agreement?.DisclosingSignature);
    public string ReceivingStatus => SignedText(Agreement?.ReceivingSignature);

    private static string SignedText(SignatureRecord? s) =>
        s is null ? "Awaiting signature" : $"Signed {s.SignedAt:MMM d, yyyy 'at' h:mm tt}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && Guid.TryParse(id?.ToString(), out var guid))
            _ = LoadAsync(guid);
        _promptPending = query.TryGetValue("new", out var n) && n?.ToString() == "true";
        query.Clear();
    }

    private async Task LoadAsync(Guid id)
    {
        Agreement = await store.GetAsync(id);
        Refresh();
    }

    /// <summary>Called when the page appears; walks both parties through signing for a newly created agreement.</summary>
    public async Task OnAppearingAsync()
    {
        if (!_promptPending) return;
        _promptPending = false;

        // Allow the load triggered by navigation to complete.
        for (var i = 0; i < 20 && Agreement is null; i++) await Task.Delay(50);
        if (Agreement is null) return;

        var page = Shell.Current.CurrentPage;
        var signNow = await page.DisplayAlertAsync(
            "Agreement Generated",
            $"Agreement {Agreement.ReferenceNumber} has been generated and saved to this device.\n\n" +
            $"Both parties must now execute the Agreement. {Agreement.DisclosingParty.Name}, as the Disclosing Party you will sign first.",
            "Sign Now", "Later");
        if (!signNow) return;

        if (!await SignAsync(disclosing: true)) return;
        await PromptReceivingPartyAsync();
    }

    private async Task PromptReceivingPartyAsync()
    {
        if (Agreement is null || Agreement.ReceivingSignature is not null) return;
        var page = Shell.Current.CurrentPage;
        var handOver = await page.DisplayAlertAsync(
            "Receiving Party Signature Required",
            $"Please hand this device to {Agreement.ReceivingParty.Name} so they may review and execute the Agreement.",
            "Continue", "Later");
        if (handOver) await SignAsync(disclosing: false);
    }

    [RelayCommand]
    private async Task SignDisclosingAsync()
    {
        if (await SignAsync(disclosing: true)) await PromptReceivingPartyAsync();
    }

    [RelayCommand]
    private Task SignReceivingAsync() => SignAsync(disclosing: false);

    private async Task<bool> SignAsync(bool disclosing)
    {
        if (Agreement is null) return false;
        var party = disclosing ? Agreement.DisclosingParty : Agreement.ReceivingParty;
        var role = disclosing ? "Disclosing Party" : "Receiving Party";

        var record = await SignaturePage.CaptureAsync(Agreement, role, party.Name, documents);
        if (record is null) return false;

        record.SignedName = party.Name;
        record.SignedAt = DateTime.Now;
        record.Device = DocumentService.DeviceDescription;
        record.DocumentFingerprint = NdaComposer.Compose(Agreement).Fingerprint;
        if (disclosing) Agreement.DisclosingSignature = record;
        else Agreement.ReceivingSignature = record;

        IsBusy = true;
        try
        {
            await documents.GenerateAsync(Agreement);
            await store.SaveAsync(Agreement);
        }
        finally
        {
            IsBusy = false;
        }
        Refresh();

        if (Agreement.IsFullyExecuted)
        {
            var share = await Shell.Current.CurrentPage.DisplayAlertAsync(
                "Agreement Executed",
                $"Agreement {Agreement.ReferenceNumber} has been fully executed by both parties. The executed copy is saved on this device.",
                "Share Copy", "Done");
            if (share) await ShareAsync();
        }
        return true;
    }

    [RelayCommand]
    private async Task ViewDocumentAsync()
    {
        if (Agreement is null) return;
        await Shell.Current.Navigation.PushAsync(new DocumentPreviewPage(Agreement, documents));
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        if (Agreement is null) return;
        var path = await documents.GetShareableCopyAsync(Agreement);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Non-Disclosure Agreement {Agreement.ReferenceNumber}",
            File = new ShareFile(path, "application/pdf")
        });
    }

    [RelayCommand]
    private async Task EmailAsync()
    {
        if (Agreement is null) return;
        if (!Email.Default.IsComposeSupported)
        {
            await ShareAsync();
            return;
        }

        var path = await documents.GetShareableCopyAsync(Agreement);
        var executed = Agreement.IsFullyExecuted;
        var message = new EmailMessage
        {
            To = [Agreement.ReceivingParty.Email],
            Subject = executed
                ? $"Executed Non-Disclosure Agreement {Agreement.ReferenceNumber}"
                : $"Non-Disclosure Agreement {Agreement.ReferenceNumber} — Signature Requested",
            Body = executed
                ? $"Dear {Agreement.ReceivingParty.Name},\n\nPlease find attached your copy of the fully executed Non-Disclosure Agreement ({Agreement.TemplateTitle}). Kindly retain it for your records.\n\nRegards,\n{Agreement.DisclosingParty.Name}"
                : $"Dear {Agreement.ReceivingParty.Name},\n\nPlease find attached Non-Disclosure Agreement {Agreement.ReferenceNumber} ({Agreement.TemplateTitle}) for your review. Your signature is requested at your earliest convenience.\n\nRegards,\n{Agreement.DisclosingParty.Name}",
            Attachments = [new EmailAttachment(path)]
        };
        await Email.Default.ComposeAsync(message);
    }

    [RelayCommand]
    private async Task TextAsync()
    {
        if (Agreement is null) return;
        if (!Sms.Default.IsComposeSupported)
        {
            await Shell.Current.CurrentPage.DisplayAlertAsync("Text Messaging Unavailable",
                "This device cannot compose text messages. Use Email or Share PDF instead.", "OK");
            return;
        }
        var body = Agreement.IsFullyExecuted
            ? $"Hi {Agreement.ReceivingParty.Name.Split(' ')[0]}, the NDA ({Agreement.ReferenceNumber}) is now fully executed. Remember: not a word."
            : $"Hi {Agreement.ReceivingParty.Name.Split(' ')[0]}, your signature is requested on Non-Disclosure Agreement {Agreement.ReferenceNumber}. Please see me at your earliest convenience.";
        await Sms.Default.ComposeAsync(new SmsMessage(body, Agreement.ReceivingParty.Phone));
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Agreement is null) return;
        var confirmed = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Delete Agreement",
            $"Permanently delete {Agreement.ReferenceNumber} and its document from this device? This cannot be undone.",
            "Delete", "Cancel");
        if (!confirmed) return;
        documents.Delete(Agreement);
        await store.DeleteAsync(Agreement.Id);
        await Shell.Current.GoToAsync("..");
    }

    private void Refresh()
    {
        // Agreement is a plain model; re-announce its dependent properties after mutation.
        OnPropertyChanged(nameof(Agreement));
        OnPropertyChanged(nameof(DisclosingStatus));
        OnPropertyChanged(nameof(ReceivingStatus));
    }
}
