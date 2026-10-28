using System.Text.Json.Serialization;

namespace PanificadoraVendas.Models;

public class Cliente
{
    public int Id { get; set; }                 // negativo = cadastrado no aparelho, ainda sem ID do servidor
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "PR";
    public int DiaVisita { get; set; }          // 0=Domingo ... 6=Sábado
    public int Sequencia { get; set; }          // 10, 20, 30... (permite inserir 15 no meio)
    public decimal SaldoDevedor { get; set; }
    public bool Ativo { get; set; } = true;
    public bool Novo { get; set; }
    public bool EnviadoServidor { get; set; }

    [JsonIgnore] public string Nome => string.IsNullOrWhiteSpace(NomeFantasia) ? RazaoSocial : NomeFantasia;
    [JsonIgnore] public string LocalTexto => $"{Endereco}{(string.IsNullOrWhiteSpace(Bairro) ? "" : " - " + Bairro)} • {Cidade}/{Uf}";
    [JsonIgnore] public bool TemDivida => SaldoDevedor > 0;
    [JsonIgnore] public string DividaTexto => TemDivida ? $"Deve R$ {SaldoDevedor:N2}" : "Sem débitos";
    [JsonIgnore] public Color CorDivida => Color.FromArgb(TemDivida ? "#C62828" : "#2E7D32");
}

public class Produto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public string Grupo { get; set; } = "";     // Fermento, Farinha, Mistura, Margarina...
    public string Unidade { get; set; } = "UN";
    public decimal Preco { get; set; }
    public decimal PrecoPromocional { get; set; }
    public bool EmPromocao { get; set; }
    public bool ProdutoNovo { get; set; }
    public bool Ativo { get; set; } = true;

    [JsonIgnore] public decimal PrecoVenda => EmPromocao && PrecoPromocional > 0 ? PrecoPromocional : Preco;
}

public class UltimaCompra
{
    public int ClienteId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime Data { get; set; }
}

public class ParcelaCondicao
{
    public int Numero { get; set; }
    public int Dias { get; set; }
    public decimal Percentual { get; set; }
    public string FormaPagamento { get; set; } = "Boleto bancário";
}

public class CondicaoPagamento
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public decimal MultaPercentual { get; set; }
    public decimal JurosPercentual { get; set; }
    public decimal DescontoPercentual { get; set; }
    public bool Ativo { get; set; } = true;
    public List<ParcelaCondicao> Parcelas { get; set; } = new();
}

public class PedidoItem
{
    public int ProdutoId { get; set; }
    public string Descricao { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    [JsonIgnore] public decimal Total => Quantidade * PrecoUnitario;
}

public class Pedido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ClienteId { get; set; }
    public string ClienteNome { get; set; } = "";
    public DateTime DataHora { get; set; } = DateTime.Now;
    public int CondicaoPagamentoId { get; set; }
    public string CondicaoDescricao { get; set; } = "";
    public List<PedidoItem> Itens { get; set; } = new();
    public bool Enviado { get; set; }
    public DateTime? DataEnvio { get; set; }

    [JsonIgnore] public decimal Total => Itens.Sum(i => i.Total);
    [JsonIgnore] public bool Pendente => !Enviado;
    [JsonIgnore] public string TotalTexto => $"R$ {Total:N2}";
    [JsonIgnore] public string Lote => DataHora.Hour < 12 ? "Lote das 12h" : "Lote das 19h";
    [JsonIgnore] public string StatusTexto => Enviado ? $"Enviado {DataEnvio:dd/MM HH:mm}" : "Pendente de envio";
    [JsonIgnore] public Color CorStatus => Enviado ? Colors.Green : Color.FromArgb("#E65100");
    [JsonIgnore] public string DetalheTexto => $"{DataHora:dd/MM HH:mm} • {Itens.Sum(i => i.Quantidade)} itens • {Lote}";
}

public class Visita
{
    public int ClienteId { get; set; }
    public DateTime Data { get; set; }
    public bool ComPedido { get; set; }
}

public class DadosLocais
{
    public List<Cliente> Clientes { get; set; } = new();
    public List<Produto> Produtos { get; set; } = new();
    public List<UltimaCompra> UltimasCompras { get; set; } = new();
    public List<CondicaoPagamento> Condicoes { get; set; } = new();
    public List<Pedido> Pedidos { get; set; } = new();
    public List<Visita> Visitas { get; set; } = new();
    public DateTime? UltimaSincronizacao { get; set; }
    public int VendedorId { get; set; }          // dono dos dados deste aparelho (0 = dados de demonstração)
}

// Contratos da API
public class SyncDto
{
    public List<Cliente> Clientes { get; set; } = new();
    public List<Produto> Produtos { get; set; } = new();
    public List<UltimaCompra> UltimasCompras { get; set; } = new();
    public List<CondicaoPagamento> Condicoes { get; set; } = new();
}

public class EnvioDto
{
    public int VendedorId { get; set; }
    public List<Cliente> ClientesNovos { get; set; } = new();
    public List<Pedido> Pedidos { get; set; } = new();
}

public class PosicaoOpcao
{
    public string Texto { get; set; } = "";
    public int? AposClienteId { get; set; }
    public bool Inicio { get; set; }
}
