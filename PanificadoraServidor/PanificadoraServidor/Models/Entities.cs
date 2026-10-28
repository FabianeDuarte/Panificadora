namespace PanificadoraServidor.Models;

public interface IEntity { int Id { get; set; } }
public interface IAtivo { bool Ativo { get; set; } }

public class Estado : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Sigla { get; set; } = "";
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
}

public class Cidade : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int EstadoId { get; set; }
    public bool Ativo { get; set; } = true;
}

public class GrupoProduto : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";   // Fermento, Farinha, Mistura, Margarina...
    public bool Ativo { get; set; } = true;
}

public class Produto : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public int GrupoProdutoId { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal Preco { get; set; }
    public decimal PrecoPromocional { get; set; }
    public bool EmPromocao { get; set; }
    public bool ProdutoNovo { get; set; }
    public int Estoque { get; set; }
    public bool Ativo { get; set; } = true;
}

public class Vendedor : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public bool Ativo { get; set; } = true;
}

/// <summary>Praça = cidade atendida por um vendedor. Só um vendedor ativo por praça.</summary>
public class VendedorPraca : IEntity
{
    public int Id { get; set; }
    public int VendedorId { get; set; }
    public int CidadeId { get; set; }
}

public class Cliente : IEntity, IAtivo
{
    public int Id { get; set; }
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public int CidadeId { get; set; }
    public bool Ativo { get; set; } = true;
}

/// <summary>Roteiro do vendedor: qual cliente, em qual dia da semana e em que sequência (10, 20, 30...).</summary>
public class RoteiroItem : IEntity
{
    public int Id { get; set; }
    public int VendedorId { get; set; }
    public int ClienteId { get; set; }
    public int DiaSemana { get; set; }     // 0=Domingo ... 6=Sábado
    public int Sequencia { get; set; }
}

public class FormaPagamento : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";    // Boleto bancário, Pix...
    public bool Ativo { get; set; } = true;
}

public class CondicaoPagamento : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Descricao { get; set; } = "";
    public decimal MultaPercentual { get; set; }
    public decimal JurosPercentual { get; set; }
    public decimal DescontoPercentual { get; set; }
    public bool Ativo { get; set; } = true;
    public List<CondicaoParcela> Parcelas { get; set; } = new();
}

public class CondicaoParcela : IEntity
{
    public int Id { get; set; }
    public int CondicaoPagamentoId { get; set; }
    public int Numero { get; set; }
    public int Dias { get; set; }
    public decimal Percentual { get; set; }
    public int FormaPagamentoId { get; set; }
}

public class Pedido : IEntity
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }                  // identifica o pedido vindo do app (evita duplicar no reenvio)
    public int VendedorId { get; set; }
    public int ClienteId { get; set; }
    public DateTime DataHora { get; set; }
    public int CondicaoPagamentoId { get; set; }
    public string Status { get; set; } = "Recebido"; // Recebido, Faturado, Cancelado
    public int? VendaId { get; set; }
    public List<PedidoItem> Itens { get; set; } = new();
}

public class PedidoItem : IEntity
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int ProdutoId { get; set; }
    public string Descricao { get; set; } = "";
    public int QtdPedida { get; set; }
    public int QtdLiberada { get; set; }            // quantidade após o corte
    public decimal PrecoUnitario { get; set; }
}

public class Venda : IEntity
{
    public int Id { get; set; }
    public int Numero { get; set; }                 // número da nota
    public int PedidoId { get; set; }
    public int ClienteId { get; set; }
    public int CondicaoPagamentoId { get; set; }
    public DateTime DataVenda { get; set; }
    public decimal Total { get; set; }
}

public class VendaItem : IEntity
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public class ContaReceber : IEntity
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int ClienteId { get; set; }
    public string Documento { get; set; } = "";     // ex.: 123/1, 123/2
    public int Parcela { get; set; }
    public DateTime Vencimento { get; set; }
    public decimal Valor { get; set; }
    public int FormaPagamentoId { get; set; }
    public decimal MultaPct { get; set; }
    public decimal JurosPct { get; set; }
    public decimal DescontoPct { get; set; }
    public bool Pago { get; set; }
    public DateTime? DataPagamento { get; set; }
    public decimal ValorPago { get; set; }
}

public class Usuario : IEntity, IAtivo
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Login { get; set; } = "";          // sempre minúsculo
    public string SenhaHash { get; set; } = "";      // PBKDF2 (salt:hash), nunca a senha
    public string Perfil { get; set; } = "Vendedor"; // Admin = site | Vendedor = aplicativo
    public int? VendedorId { get; set; }
    public bool Ativo { get; set; } = true;
    public int Falhas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
}

public class Sessao
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = "";      // SHA-256 do token; o token em si não fica no banco
    public int UsuarioId { get; set; }
    public DateTime Expira { get; set; }
}
