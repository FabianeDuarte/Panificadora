using System.Windows.Input;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

public class SincronizarViewModel : BaseViewModel
{
    readonly LocalStore _store;
    readonly ApiService _api;
    readonly AuthService _auth;
    string _apiUrl, _status = "", _info = "", _usuario = "";

    public ICommand BaixarCommand { get; }
    public ICommand SalvarCommand { get; }
    public ICommand SairCommand { get; }

    public string ApiUrl { get => _apiUrl; set => SetProperty(ref _apiUrl, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string Info { get => _info; set => SetProperty(ref _info, value); }
    public string Usuario { get => _usuario; set => SetProperty(ref _usuario, value); }

    public SincronizarViewModel(LocalStore store, ApiService api, AuthService auth)
    {
        _store = store; _api = api; _auth = auth;
        _apiUrl = store.ApiUrl;
        BaixarCommand = new Command(async () => await BaixarAsync());
        SalvarCommand = new Command(Salvar);
        SairCommand = new Command(async () => await SairAsync());
        AtualizarInfo();
    }

    public void AtualizarInfo()
    {
        var d = _store.Dados;
        Usuario = $"Vendedor: {_auth.NomeVendedor} ({_auth.Usuario})" + (_auth.Offline ? " • modo offline" : "");
        Info = (_store.ModoDemo ? "Modo DEMONSTRAÇÃO (sem servidor configurado)" : "Servidor configurado")
             + $"\nÚltima atualização: {(d.UltimaSincronizacao?.ToString("dd/MM/yyyy HH:mm") ?? "nunca")}"
             + $"\n{d.Clientes.Count} clientes • {d.Produtos.Count} produtos • {d.Condicoes.Count} condições de pagamento";
    }

    void Salvar()
    {
        _store.ApiUrl = (ApiUrl ?? "").Trim();
        Status = "Configuração salva.";
        AtualizarInfo();
    }

    async Task BaixarAsync()
    {
        if (IsBusy) return;
        Salvar();
        IsBusy = true; Status = "Baixando lista de preços e clientes...";
        var (ok, msg) = await _api.BaixarAsync();
        Status = (ok ? "✔ " : "✖ ") + msg;
        AtualizarInfo();
        IsBusy = false;
    }

    async Task SairAsync()
    {
        var d = _store.Dados;
        int pend = d.Pedidos.Count(p => !p.Enviado);
        var aviso = pend > 0
            ? $"Há {pend} pedido(s) ainda não enviado(s). Eles continuam salvos no aparelho, mas só poderão ser enviados quando você entrar de novo. Sair mesmo assim?"
            : "Deseja sair?";
        if (!await Shell.Current.DisplayAlertAsync("Sair", aviso, "Sair", "Cancelar")) return;
        _auth.Sair();
        App.MostrarLogin();
    }
}
