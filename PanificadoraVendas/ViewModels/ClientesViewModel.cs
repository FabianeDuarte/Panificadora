using System.Collections.ObjectModel;
using System.Windows.Input;
using PanificadoraVendas.Models;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

public class ClientesViewModel : BaseViewModel
{
    readonly LocalStore _store;
    string _busca = "";

    public ObservableCollection<Cliente> Lista { get; } = new();
    public ICommand AbrirCommand { get; }
    public ICommand NovoCommand { get; }

    public string Busca { get => _busca; set { if (SetProperty(ref _busca, value)) Carregar(); } }

    public ClientesViewModel(LocalStore store)
    {
        _store = store;
        AbrirCommand = new Command<Cliente>(async c => await Shell.Current.GoToAsync($"pedido?clienteId={c.Id}"));
        NovoCommand = new Command(async () => await Shell.Current.GoToAsync("clienteform"));
    }

    public void Carregar()
    {
        Lista.Clear();
        var b = (_busca ?? "").Trim();
        var q = _store.Dados.Clientes.Where(c => c.Ativo);
        if (b.Length > 0)
            q = q.Where(c => c.Nome.Contains(b, StringComparison.OrdinalIgnoreCase)
                          || c.RazaoSocial.Contains(b, StringComparison.OrdinalIgnoreCase)
                          || c.Cidade.Contains(b, StringComparison.OrdinalIgnoreCase)
                          || c.CpfCnpj.Contains(b));
        foreach (var c in q.OrderBy(c => c.Nome)) Lista.Add(c);
    }
}
