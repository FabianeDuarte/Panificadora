using PanificadoraVendas.Services;
using PanificadoraVendas.Views;

namespace PanificadoraVendas;

public partial class App : Application
{
    public App() => InitializeComponent();

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var auth = IPlatformApplication.Current!.Services.GetRequiredService<AuthService>();
        Page inicial = auth.Logado ? new AppShell() : IPlatformApplication.Current.Services.GetRequiredService<LoginPage>();
        return new Window(inicial);
    }

    /// <summary>Depois do login, troca a tela raiz para o app (abas).</summary>
    public static void MostrarShell() => Trocar(new AppShell());

    /// <summary>Ao sair, volta para a tela de login.</summary>
    public static void MostrarLogin() => Trocar(IPlatformApplication.Current!.Services.GetRequiredService<LoginPage>());

    static void Trocar(Page pagina)
    {
        var janela = Current?.Windows.FirstOrDefault();
        if (janela != null) janela.Page = pagina;
    }
}
