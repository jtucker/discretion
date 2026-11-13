using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discretion.Models;
using Discretion.Services;

namespace Discretion.ViewModels;

/// <summary>Drives the seven-step "Create New NDA" wizard.</summary>
public sealed partial class NewNdaViewModel(
    AgreementStore store,
    ProfileService profile,
    DocumentService documents) : ObservableObject
{
    public const int StepCount = 7;

    private static readonly (string Title, string Subtitle)[] Steps =
    [
        ("Select a Template", "Choose the agreement that best describes the matter requiring discretion."),
        ("Term of Agreement", "How long will this Agreement remain in effect?"),
        ("Survival of Obligations", "Must the Receiving Party maintain confidentiality after the Agreement expires?"),
        ("Receiving Party", "Who is being entrusted with the Confidential Information?"),
        ("Governing Law", "Which state's laws shall govern this Agreement?"),
        ("Effective Date", "On what date does this Agreement take effect?"),
        ("Review & Save", "Confirm the particulars before the Agreement is generated.")
    ];

    public IReadOnlyList<NdaTemplate> Templates => TemplateCatalog.All;
    public IReadOnlyList<string> States => UsStates.All;
    public IReadOnlyList<string> TermUnits { get; } = ["Months", "Years"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepTitle), nameof(StepSubtitle), nameof(StepLabel), nameof(Progress), nameof(ContinueText),
        nameof(IsStep1), nameof(IsStep2), nameof(IsStep3), nameof(IsStep4), nameof(IsStep5), nameof(IsStep6), nameof(IsStep7))]
    public partial int CurrentStep { get; set; } = 1;

    public string StepTitle => Steps[CurrentStep - 1].Title;
    public string StepSubtitle => Steps[CurrentStep - 1].Subtitle;
    public string StepLabel => $"STEP {CurrentStep} OF {StepCount}";
    public double Progress => (double)CurrentStep / StepCount;
    public string ContinueText => CurrentStep == StepCount ? "Save & Generate Agreement" : "Continue";
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool IsStep6 => CurrentStep == 6;
    public bool IsStep7 => CurrentStep == 7;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // Step 1
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial NdaTemplate? SelectedTemplate { get; set; }

    // Step 2
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TermDescription), nameof(ExpirationDescription), nameof(Summary))]
    public partial double TermLength { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TermDescription), nameof(ExpirationDescription), nameof(Summary))]
    public partial int TermUnitIndex { get; set; } = 1;

    private TermUnit Unit => TermUnitIndex == 0 ? TermUnit.Months : TermUnit.Years;
    private int Length => Math.Clamp((int)Math.Round(TermLength), 1, 99);
    public string TermDescription => Agreement.FormatTerm(Length, Unit);

    public string ExpirationDescription
    {
        get
        {
            var start = EffectiveDate ?? DateTime.Today;
            var end = Unit == TermUnit.Years ? start.AddYears(Length) : start.AddMonths(Length);
            return $"If effective {NdaComposer.LongDate(start)}, the Agreement expires {NdaComposer.LongDate(end)}.";
        }
    }

    // Step 3
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    public partial bool? SurvivesExpiration { get; set; }

    // Step 4
    [ObservableProperty] public partial string ReceivingName { get; set; } = "";
    [ObservableProperty] public partial string ReceivingPhone { get; set; } = "";
    [ObservableProperty] public partial string ReceivingEmail { get; set; } = "";
    [ObservableProperty] public partial string ReceivingStreet { get; set; } = "";
    [ObservableProperty] public partial string ReceivingCity { get; set; } = "";
    [ObservableProperty] public partial string ReceivingRegion { get; set; } = "";
    [ObservableProperty] public partial string ReceivingPostalCode { get; set; } = "";

    // Step 5
    [ObservableProperty]
    public partial string? GoverningState { get; set; }

    // Step 6
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpirationDescription))]
    public partial DateTime? EffectiveDate { get; set; } = DateTime.Today;

    public PartyInfo DisclosingParty => profile.Load();

    public IReadOnlyList<ReviewItem> Summary =>
    [
        new(1, "Template", SelectedTemplate?.Title ?? "—"),
        new(2, "Term", TermDescription),
        new(3, "Survives Expiration", SurvivesExpiration switch { true => "Yes — confidentiality continues indefinitely", false => "No — obligations end at expiration", _ => "—" }),
        new(4, "Receiving Party", BuildReceivingParty() is var rp && !string.IsNullOrWhiteSpace(rp.Name)
            ? $"{rp.Name}\n{rp.Email} · {rp.Phone}\n{rp.FormattedAddress}" : "—"),
        new(5, "Governing Law", GoverningState is null ? "—" : UsStates.Jurisdiction(GoverningState)),
        new(6, "Effective Date", EffectiveDate is { } d ? NdaComposer.LongDate(d) : "—"),
    ];

    [RelayCommand]
    private void SetTerm(string preset)
    {
        // Presets look like "6M" or "2Y".
        TermUnitIndex = preset.EndsWith('Y') ? 1 : 0;
        TermLength = int.Parse(preset[..^1]);
    }

    [RelayCommand]
    private void SetSurvival(string value) => SurvivesExpiration = value == "yes";

    [RelayCommand]
    private void SetToday() => EffectiveDate = DateTime.Today;

    [RelayCommand]
    private void GoToStep(int step)
    {
        ErrorMessage = null;
        CurrentStep = Math.Clamp(step, 1, StepCount);
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        ErrorMessage = null;
        if (CurrentStep > 1)
        {
            CurrentStep--;
            return;
        }
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        ErrorMessage = Validate(CurrentStep);
        if (ErrorMessage is not null) return;

        if (CurrentStep < StepCount)
        {
            CurrentStep++;
            if (CurrentStep == StepCount) OnPropertyChanged(nameof(Summary));
            return;
        }

        await SaveAsync();
    }

    private string? Validate(int step) => step switch
    {
        1 when SelectedTemplate is null => "Please select a template to continue.",
        3 when SurvivesExpiration is null => "Please indicate whether confidentiality survives expiration.",
        4 when string.IsNullOrWhiteSpace(ReceivingName) => "The Receiving Party's full name is required.",
        4 when !Validation.IsPhone(ReceivingPhone) => "Please enter a valid phone number for the Receiving Party.",
        4 when !Validation.IsEmail(ReceivingEmail) => "Please enter a valid email address for the Receiving Party.",
        4 when string.IsNullOrWhiteSpace(ReceivingStreet) || string.IsNullOrWhiteSpace(ReceivingCity) =>
            "Please enter the Receiving Party's street address and city.",
        5 when GoverningState is null => "Please select the governing jurisdiction.",
        6 when EffectiveDate is null => "Please select an effective date.",
        7 => Enumerable.Range(1, 6).Select(Validate).FirstOrDefault(e => e is not null),
        _ => null
    };

    private PartyInfo BuildReceivingParty() => new()
    {
        Name = ReceivingName.Trim(),
        Phone = ReceivingPhone.Trim(),
        Email = ReceivingEmail.Trim(),
        Street = ReceivingStreet.Trim(),
        City = ReceivingCity.Trim(),
        Region = ReceivingRegion.Trim(),
        PostalCode = ReceivingPostalCode.Trim()
    };

    private async Task SaveAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var effective = EffectiveDate!.Value.Date;
            var agreement = new Agreement
            {
                ReferenceNumber = NdaComposer.NewReferenceNumber(effective),
                TemplateId = SelectedTemplate!.Id,
                TemplateTitle = SelectedTemplate.Title,
                TermLength = Length,
                TermUnit = Unit,
                SurvivesExpiration = SurvivesExpiration!.Value,
                DisclosingParty = profile.Load(),
                ReceivingParty = BuildReceivingParty(),
                GoverningState = GoverningState!,
                EffectiveDate = effective,
                CreatedAt = DateTime.Now
            };

            await documents.GenerateAsync(agreement);
            await store.SaveAsync(agreement);

            // Replace the wizard with the agreement page so "back" returns to the dashboard.
            await Shell.Current.GoToAsync($"../agreement?id={agreement.Id}&new=true");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"The agreement could not be generated: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record ReviewItem(int Step, string Label, string Value);
