namespace PanificadoraServidor.Models;

// Contratos idênticos aos do aplicativo MAUI (PanificadoraVendas.Models)
public class ClienteApp
{
    public int Id { get; set; }
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "PR";
    public int DiaVisita { get; set; }
    public int Sequencia { get; set; }
    public decimal SaldoDevedor { get; set; }
    public bool Ativo { get; set; } = true;
    public bool Novo { get; set; }
    public bool EnviadoServidor { get; set; }
}

public class ProdutoApp
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string Unidade { get; set; } = "UN";
    public decimal Preco { get; set; }
    public decimal PrecoPromocional { get; set; }
    public bool EmPromocao { get; set; }
    public bool ProdutoNovo { get; set; }
    public bool Ativo { get; set; } = true;
}

public class UltimaCompraApp
{
    public int ClienteId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public DateTime Data { get; set; }
}

public class ParcelaApp
{
    public int Numero { get; set; }
    public int Dias { get; set; }
    public decimal Percentual { get; set; }
    public string FormaPagamento { get; set; } = "";
}

public class CondicaoApp
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public decimal MultaPercentual { get; set; }
    public decimal JurosPercentual { get; set; }
    public decimal DescontoPercentual { get; set; }
    public bool Ativo { get; set; } = true;
    public List<ParcelaApp> Parcelas { get; set; } = new();
}

public class DadosApp
{
    public List<ClienteApp> Clientes { get; set; } = new();
    public List<ProdutoApp> Produtos { get; set; } = new();
    public List<UltimaCompraApp> UltimasCompras { get; set; } = new();
    public List<CondicaoApp> Condicoes { get; set; } = new();
}

public class PedidoItemApp
{
    public int ProdutoId { get; set; }
    public string Descricao { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public class PedidoApp
{
    public Guid Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataHora { get; set; }
    public int CondicaoPagamentoId { get; set; }
    public List<PedidoItemApp> Itens { get; set; } = new();
}

public class EnvioApp
{
    public int VendedorId { get; set; }
    public List<ClienteApp> ClientesNovos { get; set; } = new();
    public List<PedidoApp> Pedidos { get; set; } = new();
}

public record LiberadoDto(int ItemId, int QtdLiberada);
public record BaixaDto(DateTime? Data);

public record LoginDto(string Login, string Senha);
public record TrocarSenhaDto(string SenhaAtual, string NovaSenha);
public record UsuarioDto(string Nome, string Login, string? Senha, string Perfil, int? VendedorId, bool Ativo);
