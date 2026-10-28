using System.Collections.ObjectModel;
using System.Windows.Input;
using PanificadoraVendas.Models;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

public class ProdutoLinha : BindableBase
{
    int _qtd;
    public Produto Produto { get; }
    public UltimaCompra? Ultima { get; }
    public Action? QtdAlterada { get; set; }

    public ProdutoLinha(Produto p, UltimaCompra? u) { Produto = p; Ultima = u; }

    public int Qtd
    {
        get => _qtd;
        set
        {
            value = Math.Max(0, value);
            if (!SetProperty(ref _qtd, value)) return;
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(SubtotalTexto));
            QtdAlterada?.Invoke();
        }
    }

    public string Descricao => Produto.Descricao;
    public string Grupo => Produto.Grupo;
    public int UltimaQtd => Ultima?.Quantidade ?? 0;
    public decimal Subtotal => Qtd * Produto.PrecoVenda;
    public string SubtotalTexto => Qtd > 0 ? $"R$ {Subtotal:N2}" : "";
    public string PrecoTexto => Produto.EmPromocao ? $"R$ {Produto.PrecoVenda:N2} (de R$ {Produto.Preco:N2})" : $"R$ {Produto.Preco:N2}";
    public string UltimaTexto => Ultima == null ? "Nunca comprou" : $"Última compra: {Ultima.Quantidade} un em {Ultima.Data:dd/MM} (R$ {Ultima.PrecoUnitario:N2})";
    public string Tag => Produto.EmPromocao ? "PROMOÇÃO" : Produto.ProdutoNovo ? "NOVO" : "";
    public bool TemTag => Tag.Length > 0;
    public Color CorTag => Color.FromArgb(Produto.EmPromocao ? "#E65100" : "#1565C0");
    public Color Fundo => Color.FromArgb(Produto.EmPromocao ? "#FFE0B2" : Produto.ProdutoNovo ? "#BBDEFB" : "#FFFFFF");
}

[QueryProperty(nameof(ClienteIdTexto), "clienteId")]
public class PedidoViewModel : BaseViewModel
{
    readonly LocalStore _store;
    readonly List<ProdutoLinha> _todas = new();
    CondicaoPagamento? _condicao;
    bool _mostrarTodos;

    public string ClienteIdTexto { get; set; } = "";
    public Cliente? Cliente { get; private set; }
    public ObservableCollection<ProdutoLinha> Linhas { get; } = new();
    public List<CondicaoPagamento> Condicoes { get; private set; } = new();

    public ICommand MaisCommand { get; }
    public ICommand MenosCommand { get; }
    public ICommand RepetirCommand { get; }
    public ICommand SalvarCommand { get; }
    public ICommand SemPedidoCommand { get; }

    public CondicaoPagamento? Condicao { get => _condicao; set { if (SetProperty(ref _condicao, value)) AtualizarTotais(); } }
    public bool MostrarTodos { get => _mostrarTodos; set { if (SetProperty(ref _mostrarTodos, value)) Filtrar(); } }

    public string Cabecalho => Cliente?.Nome ?? "";
    public string SubCabecalho => Cliente?.LocalTexto ?? "";
    public string DividaTexto => Cliente?.DividaTexto ?? "";
    public Color CorDivida => Cliente?.CorDivida ?? Colors.Gray;
    public decimal Total => _todas.Sum(l => l.Subtotal);
    public string TotalTexto => $"Total: R$ {Total:N2}  ({_todas.Sum(l => l.Qtd)} itens)";
    public string ParcelasTexto => Condicao == null || Total <= 0 ? "" :
        string.Join("\n", FinanceiroService.GerarParcelas(Total, Condicao, DateTime.Today)
            .Select(p => $"{p.Numero}/{Condicao.Parcelas.Count} • {p.Vencimento:dd/MM/yyyy} • R$ {p.Valor:N2} • {p.Forma}"));

    public PedidoViewModel(LocalStore store)
    {
        _store = store;
        MaisCommand = new Command<ProdutoLinha>(l => l.Qtd++);
        MenosCommand = new Command<ProdutoLinha>(l => l.Qtd--);
        RepetirCommand = new Command(() => { foreach (var l in _todas.Where(l => l.UltimaQtd > 0)) l.Qtd = l.UltimaQtd; });
        SalvarCommand = new Command(async () => await SalvarAsync());
        SemPedidoCommand = new Command(async () => await SemPedidoAsync());
    }

    public void Carregar()
    {
        if (_todas.Count > 0 || !int.TryParse(ClienteIdTexto, out var id)) return;
        var d = _store.Dados;
        Cliente = d.Clientes.FirstOrDefault(c => c.Id == id);
        if (Cliente == null) return;
        Condicoes = d.Condicoes.Where(c => c.Ativo).ToList();

        foreach (var p in d.Produtos.Where(p => p.Ativo))
        {
            var u = d.UltimasCompras.Where(x => x.ClienteId == id && x.ProdutoId == p.Id).OrderByDescending(x => x.Data).FirstOrDefault();
            _todas.Add(new ProdutoLinha(p, u) { QtdAlterada = AtualizarTotais });
        }
        Condicao = Condicoes.FirstOrDefault();
        OnPropertyChanged(string.Empty);
        Filtrar();
    }

    void Filtrar()
    {
        Linhas.Clear();
        var q = _todas.Where(l => MostrarTodos || l.UltimaQtd > 0 || l.Produto.EmPromocao || l.Produto.ProdutoNovo || l.Qtd > 0)
                      .OrderByDescending(l => l.Produto.EmPromocao || l.Produto.ProdutoNovo)
                      .ThenBy(l => l.Grupo).ThenBy(l => l.Descricao);
        foreach (var l in q) Linhas.Add(l);
    }

    void AtualizarTotais()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(ParcelasTexto));
    }

    static string LoteTexto() => DateTime.Now.Hour < 12 ? "do meio-dia (12h)" : "das 19h";

    async Task SalvarAsync()
    {
        if (Cliente == null) return;
        var itens = _todas.Where(l => l.Qtd > 0).Select(l => new PedidoItem
        { ProdutoId = l.Produto.Id, Descricao = l.Descricao, Quantidade = l.Qtd, PrecoUnitario = l.Produto.PrecoVenda }).ToList();
        if (itens.Count == 0) { await Shell.Current.DisplayAlertAsync("Pedido", "Informe a quantidade de pelo menos um produto.", "OK"); return; }
        if (Condicao == null) { await Shell.Current.DisplayAlertAsync("Pedido", "Selecione a condição de pagamento. Baixe os dados na aba Sincronizar.", "OK"); return; }

        _store.Dados.Pedidos.Add(new Pedido
        {
            ClienteId = Cliente.Id, ClienteNome = Cliente.Nome, DataHora = DateTime.Now,
            CondicaoPagamentoId = Condicao.Id, CondicaoDescricao = Condicao.Descricao, Itens = itens
        });
        _store.Dados.Visitas.Add(new Visita { ClienteId = Cliente.Id, Data = DateTime.Today, ComPedido = true });
        await _store.SalvarAsync();
        await Shell.Current.DisplayAlertAsync("Pedido salvo", $"Total R$ {Total:N2}. Será enviado no lote {LoteTexto()}.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    async Task SemPedidoAsync()
    {
        if (Cliente == null) return;
        _store.Dados.Visitas.Add(new Visita { ClienteId = Cliente.Id, Data = DateTime.Today, ComPedido = false });
        await _store.SalvarAsync();
        await Shell.Current.GoToAsync("..");
    }
}
