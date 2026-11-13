using Discretion.ViewModels;

namespace Discretion.Views;

public partial class AgreementPage : ContentPage
{
    private readonly AgreementViewModel _vm;

    public AgreementPage(AgreementViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.OnAppearingAsync();
    }
}
