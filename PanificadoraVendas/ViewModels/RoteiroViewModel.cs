using System.Collections.ObjectModel;
using System.Windows.Input;
using PanificadoraVendas.Models;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

public class ClienteRotaItem
{
    public Cliente Cliente { get; init; } = null!;
    public bool Visitado { get; init; }
    public bool ComPedido { get; init; }
    public string Status => !Visitado ? "A visitar" : ComPedido ? "✔ Pedido feito" : "Sem pedido";
    public Color CorStatus => !Visitado ? Colors.Gray : ComPedido ? Colors.Green : Colors.Orange;
}

public class RoteiroViewModel : BaseViewModel
{
    readonly LocalStore _store;
    int _diaIndex = (int)DateTime.Today.DayOfWeek;
    string _resumo = "";

    public string[] Dias { get; } = { "Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado" };
    public ObservableCollection<ClienteRotaItem> Clientes { get; } = new();
    public ICommand AbrirCommand { get; }
    public ICommand ProximoCommand { get; }
    public ICommand NovoClienteCommand { get; }

    public int DiaIndex
    {
        get => _diaIndex;
        set { if (value < 0) return; if (SetProperty(ref _diaIndex, value)) Carregar(); }
    }

    public string Resumo { get => _resumo; set => SetProperty(ref _resumo, value); }

    public RoteiroViewModel(LocalStore store)
    {
        _store = store;
        AbrirCommand = new Command<ClienteRotaItem>(async i => await Shell.Current.GoToAsync($"pedido?clienteId={i.Cliente.Id}"));
        ProximoCommand = new Command(async () =>
        {
            var prox = Clientes.FirstOrDefault(c => !c.Visitado);
            if (prox == null) { await Shell.Current.DisplayAlertAsync("Roteiro", "Todos os clientes deste dia já foram visitados.", "OK"); return; }
            await Shell.Current.GoToAsync($"pedido?clienteId={prox.Cliente.Id}");
        });
        NovoClienteCommand = new Command(async () => await Shell.Current.GoToAsync("clienteform"));
    }

    public void Carregar()
    {
        Clientes.Clear();
        var hoje = DateTime.Today;
        bool diaDeHoje = _diaIndex == (int)hoje.DayOfWeek;
        foreach (var c in RoteiroService.RotaDoDia(_store.Dados.Clientes, _diaIndex))
        {
            var v = diaDeHoje ? _store.Dados.Visitas.LastOrDefault(x => x.ClienteId == c.Id && x.Data.Date == hoje) : null;
            Clientes.Add(new ClienteRotaItem { Cliente = c, Visitado = v != null, ComPedido = v?.ComPedido ?? false });
        }
        Resumo = diaDeHoje
            ? $"{Clientes.Count(x => x.Visitado)} de {Clientes.Count} clientes visitados hoje"
            : $"{Clientes.Count} clientes neste dia";
        OnPropertyChanged(nameof(DiaIndex));
    }
}
