using System.Windows.Input;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

public class LoginViewModel : BaseViewModel
{
    readonly AuthService _auth;
    readonly LocalStore _store;
    string _login, _senha = "", _apiUrl, _erro = "";

    public ICommand EntrarCommand { get; }

    public string Login { get => _login; set => SetProperty(ref _login, value); }
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }
    public string ApiUrl { get => _apiUrl; set => SetProperty(ref _apiUrl, value); }
    public string Erro { get => _erro; set => SetProperty(ref _erro, value); }

    public LoginViewModel(AuthService auth, LocalStore store)
    {
        _auth = auth; _store = store;
        _login = auth.UltimoLogin;
        _apiUrl = store.ApiUrl;
        EntrarCommand = new Command(async () => await EntrarAsync());
    }

    async Task EntrarAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Erro = "";
        var (ok, msg) = await _auth.EntrarAsync(Login, Senha, ApiUrl);
        IsBusy = false;
        if (!ok) { Erro = msg; return; }

        Senha = "";
        App.MostrarShell();
        if (!string.IsNullOrEmpty(msg))      // ex.: entrou em modo offline
            await Shell.Current.DisplayAlertAsync("Login", msg, "OK");
    }
}
