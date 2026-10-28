using PanificadoraVendas.Models;

namespace PanificadoraVendas.Services;

/// <summary>Dados de demonstração (usados enquanto não há API configurada).</summary>
public static class SeedData
{
    public static DadosLocais Criar()
    {
        int hoje = (int)DateTime.Today.DayOfWeek, outro = (hoje + 2) % 7;
        var d = new DadosLocais();

        Cliente C(int id, string fant, string razao, string cnpj, string end, string bairro, string cid, int dia, int seq, decimal deve) => new()
        {
            Id = id, NomeFantasia = fant, RazaoSocial = razao, CpfCnpj = cnpj, Endereco = end, Bairro = bairro,
            Cidade = cid, Uf = "PR", DiaVisita = dia, Sequencia = seq, SaldoDevedor = deve, Telefone = "(45) 99999-0000"
        };

        d.Clientes.AddRange(new[]
        {
            C(1, "Panificadora Engenho Doce", "Engenho Doce Ltda", "11222333000181", "Av. Paraná, 1200", "Centro", "Foz do Iguaçu", hoje, 10, 0),
            C(2, "Padaria Mufato", "Mufato & Cia ME", "22333444000172", "Rua Almirante Barroso, 850", "Centro", "Foz do Iguaçu", hoje, 20, 350.80m),
            C(3, "Padaria Pão Quente", "Pão Quente MEI", "33444555000163", "Av. Brasil, 4500", "Vila Yolanda", "Foz do Iguaçu", hoje, 30, 0),
            C(4, "Confeitaria Sabor & Arte", "Sabor e Arte Ltda", "44555666000154", "Rua Rebouças, 300", "Centro", "Foz do Iguaçu", hoje, 40, 1280.00m),
            C(5, "Padaria Santa Terezinha", "Santa Terezinha ME", "55666777000145", "Rua Principal, 20", "Centro", "Santa Terezinha de Itaipu", outro, 10, 0),
            C(6, "Padaria São Miguel", "São Miguel MEI", "66777888000136", "Av. Central, 150", "Centro", "São Miguel do Iguaçu", outro, 20, 90.00m),
        });

        Produto P(int id, string desc, string grupo, decimal preco, bool promo = false, decimal precoPromo = 0, bool novo = false) =>
            new() { Id = id, Descricao = desc, Grupo = grupo, Preco = preco, EmPromocao = promo, PrecoPromocional = precoPromo, ProdutoNovo = novo, Unidade = "UN" };

        d.Produtos.AddRange(new[]
        {
            P(1, "Fermento biológico fresco 500g", "Fermento", 12.50m),
            P(2, "Fermento instantâneo seco 500g", "Fermento", 28.90m),
            P(3, "Farinha de trigo p/ pão 25kg", "Farinha", 135.00m),
            P(4, "Farinha de trigo p/ bolo 25kg", "Farinha", 142.00m),
            P(5, "Farinha de trigo p/ salgados 25kg", "Farinha", 138.00m),
            P(6, "Margarina balde 20L", "Margarina", 189.90m, true, 159.90m),
            P(7, "Mistura p/ pão francês 5kg", "Mistura", 48.00m),
            P(8, "Mistura p/ bolo de cenoura 5kg", "Mistura", 52.00m),
            P(9, "Mistura p/ pão de queijo 1kg", "Mistura", 21.50m, false, 0, true),
            P(10, "Mistura p/ pão de queijo (Paraguai) 1kg", "Mistura", 17.90m, false, 0, true),
        });

        var h = DateTime.Today.AddDays(-7);
        UltimaCompra U(int cli, int prod, int qtd, decimal preco) => new() { ClienteId = cli, ProdutoId = prod, Quantidade = qtd, PrecoUnitario = preco, Data = h };
        d.UltimasCompras.AddRange(new[]
        {
            U(1,1,10,12.50m), U(1,3,4,135m), U(1,7,6,48m), U(1,9,2,21.5m),
            U(2,1,6,12.50m), U(2,4,2,142m), U(2,6,1,189.9m),
            U(3,2,3,28.9m), U(3,5,3,138m), U(3,8,4,52m),
            U(4,1,8,12.5m), U(4,4,5,142m), U(4,8,6,52m),
        });

        d.Condicoes.AddRange(new[]
        {
            new CondicaoPagamento { Id = 10, Descricao = "7/14 dias - Boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 7, Percentual = 50 }, new() { Numero = 2, Dias = 14, Percentual = 50 } } },
            new CondicaoPagamento { Id = 20, Descricao = "7/14/21 dias - Boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 7, Percentual = 33.33m }, new() { Numero = 2, Dias = 14, Percentual = 33.33m }, new() { Numero = 3, Dias = 21, Percentual = 33.34m } } },
            new CondicaoPagamento { Id = 30, Descricao = "Entrada Pix + 7 dias boleto", MultaPercentual = 5, JurosPercentual = 2, DescontoPercentual = 1,
                Parcelas = { new() { Numero = 1, Dias = 0, Percentual = 50, FormaPagamento = "Pix" }, new() { Numero = 2, Dias = 7, Percentual = 50 } } },
        });
        return d;
    }
}
