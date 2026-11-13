namespace Discretion;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        // The parchment-and-navy look is designed for light mode only.
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new AppShell())
        {
            Title = "Discretion",
            MinimumWidth = 420,
            MinimumHeight = 640
        };
}
