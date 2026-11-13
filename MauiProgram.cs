using Discretion.Services;
using Discretion.ViewModels;
using Discretion.Views;
using Microsoft.Extensions.Logging;

namespace Discretion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AgreementStore>();
        builder.Services.AddSingleton<ProfileService>();
        builder.Services.AddSingleton<DocumentService>();

        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<NewNdaViewModel>();
        builder.Services.AddTransient<AgreementViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<NewNdaPage>();
        builder.Services.AddTransient<AgreementPage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
