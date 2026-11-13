using Discretion.ViewModels;

namespace Discretion.Views;

public partial class NewNdaPage : ContentPage
{
    private readonly NewNdaViewModel _vm;

    public NewNdaPage(NewNdaViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Android hardware back steps backwards through the wizard rather than discarding it.
    protected override bool OnBackButtonPressed()
    {
        _vm.BackCommand.Execute(null);
        return true;
    }
}
