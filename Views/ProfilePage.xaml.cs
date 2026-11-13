using Discretion.ViewModels;

namespace Discretion.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
