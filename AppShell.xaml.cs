using Discretion.Views;

namespace Discretion;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("newnda", typeof(NewNdaPage));
        Routing.RegisterRoute("agreement", typeof(AgreementPage));
        Routing.RegisterRoute("profile", typeof(ProfilePage));
    }
}
