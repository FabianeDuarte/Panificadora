using System.Collections.ObjectModel;
using System.Windows.Input;
using PanificadoraVendas.Models;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

/// <summary>Envio em lote: pedidos feitos até 12h seguem às 12h; os da tarde seguem às 19h.</summary>
public class EnvioViewModel : BaseViewModel
{
    readonly LocalStore _store;
    readonly ApiService _api;
    string _lote12 = "", _lote19 = "", _proximo = "", _status = "";

    public ObservableCollection<Pedido> Pedidos { get; } = new();
    public ICommand EnviarCommand { get; }
    public ICommand ExcluirCommand { get; }

    public string Lote12 { get => _lote12; set => SetProperty(ref _lote12, value); }
    public string Lote19 { get => _lote19; set => SetProperty(ref _lote19, value); }
    public string Proximo { get => _proximo; set => SetProperty(ref _proximo, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public EnvioViewModel(LocalStore store, ApiService api)
    {
        _store = store; _api = api;
        EnviarCommand = new Command(async () => await EnviarAsync());
        ExcluirCommand = new Command<Pedido>(async p => await ExcluirAsync(p));
    }

    public void Carregar()
    {
        var d = _store.Dados;
        var pend = d.Pedidos.Where(p => !p.Enviado).OrderBy(p => p.DataHora).ToList();
        var a = pend.Where(p => p.DataHora.Hour < 12).ToList();
        var b = pend.Where(p => p.DataHora.Hour >= 12).ToList();
        int novos = d.Clientes.Count(c => c.Novo && !c.EnviadoServidor);

        Lote12 = $"Lote 12h: {a.Count} pedido(s) • R$ {a.Sum(p => p.Total):N2}";
        Lote19 = $"Lote 19h: {b.Count} pedido(s) • R$ {b.Sum(p => p.Total):N2}";
        var h = DateTime.Now.Hour;
        Proximo = (h < 12 ? "Próximo envio: hoje às 12:00" : h < 19 ? "Próximo envio: hoje às 19:00" : "Próximo envio: amanhã às 12:00")
                  + (novos > 0 ? $" • {novos} cliente(s) novo(s) pendente(s)" : "");

        Pedidos.Clear();
        foreach (var p in pend) Pedidos.Add(p);
        foreach (var p in d.Pedidos.Where(p => p.Enviado).OrderByDescending(p => p.DataEnvio).Take(30)) Pedidos.Add(p);
    }

    async Task EnviarAsync()
    {
        if (IsBusy) return;
        IsBusy = true; Status = "Enviando...";
        var (ok, msg) = await _api.EnviarAsync();
        Status = (ok ? "✔ " : "✖ ") + msg;
        Carregar();
        IsBusy = false;
    }

    async Task ExcluirAsync(Pedido p)
    {
        if (p.Enviado) return;
        if (!await Shell.Current.DisplayAlertAsync("Excluir", $"Excluir o pedido de {p.ClienteNome}?", "Excluir", "Cancelar")) return;
        _store.Dados.Pedidos.Remove(p);
        await _store.SalvarAsync();
        Carregar();
    }
}
