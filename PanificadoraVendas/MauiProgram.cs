using PanificadoraVendas.Services;
using PanificadoraVendas.ViewModels;
using PanificadoraVendas.Views;

namespace PanificadoraVendas;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .ConfigureFonts(f =>
            {
                f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                f.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<LocalStore>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<ApiService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RoteiroViewModel>();
        builder.Services.AddTransient<ClientesViewModel>();
        builder.Services.AddTransient<ClienteFormViewModel>();
        builder.Services.AddTransient<PedidoViewModel>();
        builder.Services.AddTransient<EnvioViewModel>();
        builder.Services.AddTransient<SincronizarViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RoteiroPage>();
        builder.Services.AddTransient<ClientesPage>();
        builder.Services.AddTransient<ClienteFormPage>();
        builder.Services.AddTransient<PedidoPage>();
        builder.Services.AddTransient<EnvioPage>();
        builder.Services.AddTransient<SincronizarPage>();

        return builder.Build();
    }
}
