using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Discretion.Models;
using Discretion.Services;

namespace Discretion.ViewModels;

public sealed partial class DashboardViewModel(AgreementStore store, ProfileService profile) : ObservableObject
{
    public ObservableCollection<Agreement> Agreements { get; } = [];

    [ObservableProperty]
    public partial int PendingCount { get; set; }

    [ObservableProperty]
    public partial int ExecutedCount { get; set; }

    [ObservableProperty]
    public partial string Greeting { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEmpty { get; set; } = true;

    [RelayCommand]
    public async Task LoadAsync()
    {
        var all = await store.GetAllAsync();
        Agreements.Clear();
        foreach (var a in all) Agreements.Add(a);
        PendingCount = all.Count(a => !a.IsFullyExecuted);
        ExecutedCount = all.Count(a => a.IsFullyExecuted);
        IsEmpty = all.Count == 0;

        var name = profile.Load().Name;
        var hour = DateTime.Now.Hour;
        var salutation = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
        Greeting = string.IsNullOrWhiteSpace(name) ? salutation : $"{salutation}, {name.Split(' ')[0]}";
    }

    [RelayCommand]
    private async Task CreateNewAsync()
    {
        if (!profile.IsComplete)
        {
            await Shell.Current.GoToAsync("profile?onboarding=true");
            return;
        }
        await Shell.Current.GoToAsync("newnda");
    }

    [RelayCommand]
    private Task OpenAsync(Agreement? agreement) =>
        agreement is null ? Task.CompletedTask : Shell.Current.GoToAsync($"agreement?id={agreement.Id}");

    [RelayCommand]
    private Task OpenProfileAsync() => Shell.Current.GoToAsync("profile");
}
