using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Models;
using PanificadoraServidor.Services;

namespace PanificadoraServidor.Endpoints;

/// <summary>API consumida pelo aplicativo MAUI do vendedor.</summary>
public static class AppEndpoints
{
    public static void MapApp(this WebApplication app)
    {
        var g = app.MapGroup("/api/app");

        // Baixar carteira de clientes (com roteiro), produtos, condições e últimas compras
        g.MapGet("/dados", async (HttpContext ctx, AppDb db) =>
        {
            var vendedorId = ctx.UsuarioLogado().VendedorId ?? 0;   // vem do login, não do cliente
            var roteiro = await db.Roteiro.Where(r => r.VendedorId == vendedorId).ToListAsync();
            var ids = roteiro.Select(r => r.ClienteId).ToList();
            var clientes = await db.Clientes.Where(c => c.Ativo && ids.Contains(c.Id)).ToListAsync();
            var cidades = await db.Cidades.ToDictionaryAsync(c => c.Id);
            var estados = await db.Estados.ToDictionaryAsync(e => e.Id);
            var deve = (await db.Contas.Where(c => !c.Pago && ids.Contains(c.ClienteId)).ToListAsync())
                       .GroupBy(c => c.ClienteId).ToDictionary(x => x.Key, x => x.Sum(c => c.Valor));

            var dto = new DadosApp();
            foreach (var c in clientes)
            {
                var r = roteiro.First(x => x.ClienteId == c.Id);
                var cid = cidades.GetValueOrDefault(c.CidadeId);
                dto.Clientes.Add(new ClienteApp
                {
                    Id = c.Id, RazaoSocial = c.RazaoSocial, NomeFantasia = c.NomeFantasia, CpfCnpj = c.CpfCnpj, Telefone = c.Telefone,
                    Endereco = c.Endereco, Bairro = c.Bairro, Cidade = cid?.Nome ?? "",
                    Uf = cid != null ? estados.GetValueOrDefault(cid.EstadoId)?.Sigla ?? "PR" : "PR",
                    DiaVisita = r.DiaSemana, Sequencia = r.Sequencia, SaldoDevedor = deve.GetValueOrDefault(c.Id), Ativo = true
                });
            }

            var grupos = await db.Grupos.ToDictionaryAsync(x => x.Id);
            dto.Produtos = (await db.Produtos.Where(p => p.Ativo).ToListAsync()).Select(p => new ProdutoApp
            {
                Id = p.Id, Descricao = p.Descricao, Grupo = grupos.GetValueOrDefault(p.GrupoProdutoId)?.Descricao ?? "", Unidade = p.Unidade,
                Preco = p.Preco, PrecoPromocional = p.PrecoPromocional, EmPromocao = p.EmPromocao, ProdutoNovo = p.ProdutoNovo, Ativo = true
            }).ToList();

            var formas = await db.Formas.ToDictionaryAsync(f => f.Id);
            dto.Condicoes = (await db.Condicoes.Include(c => c.Parcelas).Where(c => c.Ativo).ToListAsync()).Select(c => new CondicaoApp
            {
                Id = c.Id, Descricao = c.Descricao, MultaPercentual = c.MultaPercentual, JurosPercentual = c.JurosPercentual,
                DescontoPercentual = c.DescontoPercentual, Ativo = true,
                Parcelas = c.Parcelas.OrderBy(p => p.Numero).Select(p => new ParcelaApp
                { Numero = p.Numero, Dias = p.Dias, Percentual = p.Percentual, FormaPagamento = formas.GetValueOrDefault(p.FormaPagamentoId)?.Descricao ?? "" }).ToList()
            }).ToList();

            // quantidade da ÚLTIMA compra de cada produto por cliente (não precisa ser da última semana)
            var vendas = await db.Vendas.Where(v => ids.Contains(v.ClienteId)).ToListAsync();
            var vendaIds = vendas.Select(v => v.Id).ToList();
            var itens = await db.VendaItens.Where(i => vendaIds.Contains(i.VendaId)).ToListAsync();
            dto.UltimasCompras = itens
                .Join(vendas, i => i.VendaId, v => v.Id, (i, v) => new { v.ClienteId, v.DataVenda, i.ProdutoId, i.Quantidade, i.PrecoUnitario })
                .GroupBy(x => (x.ClienteId, x.ProdutoId))
                .Select(grp => grp.OrderByDescending(x => x.DataVenda).First())
                .Select(x => new UltimaCompraApp { ClienteId = x.ClienteId, ProdutoId = x.ProdutoId, Quantidade = x.Quantidade, PrecoUnitario = x.PrecoUnitario, Data = x.DataVenda })
                .ToList();

            return Results.Ok(dto);
        });

        // Receber clientes novos e pedidos (enviados às 12h e às 19h)
        g.MapPost("/pedidos", async Task<IResult> (EnvioApp env, HttpContext ctx, AppDb db) =>
        {
            var vendedorId = ctx.UsuarioLogado().VendedorId ?? 0;   // ignora env.VendedorId: vale o vendedor logado
            var avisos = new List<string>();
            var vendedor = await db.Vendedores.FirstOrDefaultAsync(v => v.Id == vendedorId && v.Ativo);
            if (vendedor == null) return Results.BadRequest(new { erro = "Vendedor inválido ou inativo." });

            var mapa = new Dictionary<int, int>();   // id temporário (negativo) -> id real

            foreach (var n in env.ClientesNovos)
            {
                var doc = new string(n.CpfCnpj.Where(char.IsDigit).ToArray());
                var existente = await db.Clientes.FirstOrDefaultAsync(c => c.CpfCnpj == doc);
                if (existente != null) { mapa[n.Id] = existente.Id; avisos.Add($"Cliente {n.RazaoSocial} já existia e foi reaproveitado."); continue; }

                var uf = string.IsNullOrWhiteSpace(n.Uf) ? "PR" : n.Uf.ToUpperInvariant();
                var estado = await db.Estados.FirstOrDefaultAsync(e => e.Sigla == uf);
                if (estado == null) { estado = new Estado { Sigla = uf, Nome = uf }; db.Estados.Add(estado); await db.SaveChangesAsync(); }
                var nomeCidade = n.Cidade.Trim();
                var cidade = await db.Cidades.FirstOrDefaultAsync(c => c.EstadoId == estado.Id && c.Nome.ToLower() == nomeCidade.ToLower());
                if (cidade == null) { cidade = new Cidade { Nome = nomeCidade, EstadoId = estado.Id }; db.Cidades.Add(cidade); await db.SaveChangesAsync(); }

                var cliente = new Cliente
                {
                    RazaoSocial = n.RazaoSocial, NomeFantasia = n.NomeFantasia, CpfCnpj = doc, Telefone = n.Telefone,
                    Endereco = n.Endereco, Bairro = n.Bairro, CidadeId = cidade.Id, Ativo = true
                };
                db.Clientes.Add(cliente);
                await db.SaveChangesAsync();
                mapa[n.Id] = cliente.Id;

                // Praça: se ninguém atende a cidade, passa a ser deste vendedor; se for de outro, o cliente fica sem roteiro.
                var dono = await AdminEndpoints.DonoDaPracaAsync(db, cidade.Id);
                if (dono == null) { db.Pracas.Add(new VendedorPraca { VendedorId = vendedor.Id, CidadeId = cidade.Id }); dono = vendedor; }
                if (dono.Id == vendedor.Id)
                    db.Roteiro.Add(new RoteiroItem { VendedorId = vendedor.Id, ClienteId = cliente.Id, DiaSemana = n.DiaVisita, Sequencia = n.Sequencia <= 0 ? 10 : n.Sequencia });
                else
                    avisos.Add($"Cliente {n.RazaoSocial}: a praça {cidade.Nome} pertence ao vendedor {dono.Nome}. Cliente cadastrado, mas sem roteiro.");
                await db.SaveChangesAsync();
            }

            int recebidos = 0;
            foreach (var p in env.Pedidos)
            {
                if (await db.Pedidos.AnyAsync(x => x.Uuid == p.Id)) continue;     // reenvio: ignora duplicado
                int clienteId = mapa.TryGetValue(p.ClienteId, out var real) ? real : p.ClienteId;
                if (!await db.Clientes.AnyAsync(c => c.Id == clienteId)) { avisos.Add($"Pedido {p.Id}: cliente {p.ClienteId} não encontrado."); continue; }
                if (p.Itens.Count == 0) continue;

                db.Pedidos.Add(new Pedido
                {
                    Uuid = p.Id, VendedorId = vendedor.Id, ClienteId = clienteId, DataHora = p.DataHora, CondicaoPagamentoId = p.CondicaoPagamentoId,
                    Status = "Recebido",
                    Itens = p.Itens.Select(i => new PedidoItem
                    { ProdutoId = i.ProdutoId, Descricao = i.Descricao, QtdPedida = i.Quantidade, QtdLiberada = i.Quantidade, PrecoUnitario = i.PrecoUnitario }).ToList()
                });
                recebidos++;
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { pedidosRecebidos = recebidos, clientesNovos = mapa.Count, avisos });
        });
    }
}
