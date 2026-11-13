using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discretion.Models;
using Discretion.Services;

namespace Discretion.ViewModels;

public sealed partial class ProfileViewModel : ObservableObject, IQueryAttributable
{
    private readonly ProfileService _profile;

    public ProfileViewModel(ProfileService profile)
    {
        _profile = profile;
        var p = profile.Load();
        Name = p.Name;
        Email = p.Email;
        Phone = p.Phone;
        Street = p.Street;
        City = p.City;
        Region = p.Region;
        PostalCode = p.PostalCode;
    }

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Email { get; set; }
    [ObservableProperty] public partial string Phone { get; set; }
    [ObservableProperty] public partial string Street { get; set; }
    [ObservableProperty] public partial string City { get; set; }
    [ObservableProperty] public partial string Region { get; set; }
    [ObservableProperty] public partial string PostalCode { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial bool IsOnboarding { get; set; }

    public string SaveText => IsOnboarding ? "Save & Continue" : "Save Details";

    partial void OnIsOnboardingChanged(bool value) => OnPropertyChanged(nameof(SaveText));

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        IsOnboarding = query.TryGetValue("onboarding", out var v) && v?.ToString() == "true";

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Your full legal name is required to act as the Disclosing Party.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(Email) && !Validation.IsEmail(Email))
        {
            ErrorMessage = "Please enter a valid email address.";
            return;
        }

        _profile.Save(new PartyInfo
        {
            Name = Name.Trim(),
            Email = Email?.Trim() ?? "",
            Phone = Phone?.Trim() ?? "",
            Street = Street?.Trim() ?? "",
            City = City?.Trim() ?? "",
            Region = Region?.Trim() ?? "",
            PostalCode = PostalCode?.Trim() ?? ""
        });
        ErrorMessage = null;
        await Shell.Current.GoToAsync(IsOnboarding ? "../newnda" : "..");
    }
}
