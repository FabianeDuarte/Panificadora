using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Models;

namespace PanificadoraServidor.Data;

public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }

    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Cidade> Cidades => Set<Cidade>();
    public DbSet<GrupoProduto> Grupos => Set<GrupoProduto>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<VendedorPraca> Pracas => Set<VendedorPraca>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<RoteiroItem> Roteiro => Set<RoteiroItem>();
    public DbSet<FormaPagamento> Formas => Set<FormaPagamento>();
    public DbSet<CondicaoPagamento> Condicoes => Set<CondicaoPagamento>();
    public DbSet<CondicaoParcela> Parcelas => Set<CondicaoParcela>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItens => Set<PedidoItem>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<VendaItem> VendaItens => Set<VendaItem>();
    public DbSet<ContaReceber> Contas => Set<ContaReceber>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sessao> Sessoes => Set<Sessao>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Pedido>().HasIndex(p => p.Uuid).IsUnique();
        b.Entity<Cliente>().HasIndex(c => c.CpfCnpj).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.Login).IsUnique();
        b.Entity<Sessao>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<RoteiroItem>().HasIndex(r => r.ClienteId).IsUnique();   // um cliente só em um roteiro
    }
}
