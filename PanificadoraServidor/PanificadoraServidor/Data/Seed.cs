using PanificadoraServidor.Models;

namespace PanificadoraServidor.Data;

public static class Seed
{
    public static void Executar(AppDb db)
    {
        if (!db.Usuarios.Any()) SeedUsuarios(db);
        if (db.Estados.Any()) return;

        db.Estados.AddRange(new Estado { Sigla = "PR", Nome = "Paraná" }, new Estado { Sigla = "SC", Nome = "Santa Catarina" });
        db.SaveChanges();
        var pr = db.Estados.First(e => e.Sigla == "PR").Id;
        db.Cidades.AddRange(
            new Cidade { Nome = "Foz do Iguaçu", EstadoId = pr },
            new Cidade { Nome = "Santa Terezinha de Itaipu", EstadoId = pr },
            new Cidade { Nome = "São Miguel do Iguaçu", EstadoId = pr },
            new Cidade { Nome = "Cascavel", EstadoId = pr });
        db.Grupos.AddRange(new GrupoProduto { Descricao = "Fermento" }, new GrupoProduto { Descricao = "Farinha" },
            new GrupoProduto { Descricao = "Mistura" }, new GrupoProduto { Descricao = "Margarina" });
        db.Formas.AddRange(new FormaPagamento { Descricao = "Boleto bancário" }, new FormaPagamento { Descricao = "Pix" });
        db.Vendedores.AddRange(new Vendedor { Nome = "Henrique" }, new Vendedor { Nome = "Marcos" });
        db.SaveChanges();

        int G(string d) => db.Grupos.First(g => g.Descricao == d).Id;
        db.Produtos.AddRange(
            new Produto { Descricao = "Fermento biológico fresco 500g", GrupoProdutoId = G("Fermento"), Preco = 12.50m, Estoque = 200 },
            new Produto { Descricao = "Fermento instantâneo seco 500g", GrupoProdutoId = G("Fermento"), Preco = 28.90m, Estoque = 80 },
            new Produto { Descricao = "Farinha de trigo p/ pão 25kg", GrupoProdutoId = G("Farinha"), Preco = 135m, Estoque = 30 },
            new Produto { Descricao = "Farinha de trigo p/ bolo 25kg", GrupoProdutoId = G("Farinha"), Preco = 142m, Estoque = 15 },
            new Produto { Descricao = "Farinha de trigo p/ salgados 25kg", GrupoProdutoId = G("Farinha"), Preco = 138m, Estoque = 20 },
            new Produto { Descricao = "Margarina balde 20L", GrupoProdutoId = G("Margarina"), Preco = 189.90m, PrecoPromocional = 159.90m, EmPromocao = true, Estoque = 25 },
            new Produto { Descricao = "Mistura p/ pão francês 5kg", GrupoProdutoId = G("Mistura"), Preco = 48m, Estoque = 40 },
            new Produto { Descricao = "Mistura p/ bolo de cenoura 5kg", GrupoProdutoId = G("Mistura"), Preco = 52m, Estoque = 40 },
            new Produto { Descricao = "Mistura p/ pão de queijo 1kg", GrupoProdutoId = G("Mistura"), Preco = 21.50m, ProdutoNovo = true, Estoque = 60 });

        var boleto = db.Formas.First(f => f.Descricao == "Boleto bancário").Id;
        var pix = db.Formas.First(f => f.Descricao == "Pix").Id;
        db.Condicoes.AddRange(
            new CondicaoPagamento { Descricao = "7/14 dias - Boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 7, Percentual = 50, FormaPagamentoId = boleto }, new() { Numero = 2, Dias = 14, Percentual = 50, FormaPagamentoId = boleto } } },
            new CondicaoPagamento { Descricao = "7/14/21 dias - Boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 7, Percentual = 33.33m, FormaPagamentoId = boleto }, new() { Numero = 2, Dias = 14, Percentual = 33.33m, FormaPagamentoId = boleto }, new() { Numero = 3, Dias = 21, Percentual = 33.34m, FormaPagamentoId = boleto } } },
            new CondicaoPagamento { Descricao = "Entrada Pix + 7 dias boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 0, Percentual = 50, FormaPagamentoId = pix }, new() { Numero = 2, Dias = 7, Percentual = 50, FormaPagamentoId = boleto } } });
        db.SaveChanges();

        var foz = db.Cidades.First(c => c.Nome == "Foz do Iguaçu").Id;
        var hen = db.Vendedores.First(v => v.Nome == "Henrique").Id;
        db.Pracas.Add(new VendedorPraca { VendedorId = hen, CidadeId = foz });
        db.Clientes.AddRange(
            new Cliente { RazaoSocial = "Engenho Doce Ltda", NomeFantasia = "Panificadora Engenho Doce", CpfCnpj = "11222333000181", Endereco = "Av. Paraná, 1200", Bairro = "Centro", CidadeId = foz },
            new Cliente { RazaoSocial = "Mufato & Cia ME", NomeFantasia = "Padaria Mufato", CpfCnpj = "22333444000172", Endereco = "Rua Almirante Barroso, 850", Bairro = "Centro", CidadeId = foz },
            new Cliente { RazaoSocial = "Pão Quente MEI", NomeFantasia = "Padaria Pão Quente", CpfCnpj = "33444555000163", Endereco = "Av. Brasil, 4500", Bairro = "Vila Yolanda", CidadeId = foz });
        db.SaveChanges();

        int seq = 10;
        foreach (var c in db.Clientes.OrderBy(c => c.Id).ToList())
        { db.Roteiro.Add(new RoteiroItem { VendedorId = hen, ClienteId = c.Id, DiaSemana = 1, Sequencia = seq }); seq += 10; }

        var mar = db.Vendedores.First(v => v.Nome == "Marcos").Id;
        db.Usuarios.Add(new Usuario { Nome = "Henrique", Login = "henrique", SenhaHash = PanificadoraServidor.Services.Senha.Hash("123456"), Perfil = "Vendedor", VendedorId = hen });
        db.Usuarios.Add(new Usuario { Nome = "Marcos", Login = "marcos", SenhaHash = PanificadoraServidor.Services.Senha.Hash("123456"), Perfil = "Vendedor", VendedorId = mar });
        db.SaveChanges();
    }

    /// <summary>Usuários iniciais. TROQUE AS SENHAS no primeiro acesso.</summary>
    static void SeedUsuarios(AppDb db)
    {
        db.Usuarios.Add(new Usuario { Nome = "Administrador", Login = "admin", SenhaHash = Services.Senha.Hash("admin123"), Perfil = "Admin" });
        db.SaveChanges();
    }
}
