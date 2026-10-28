using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Models;
using PanificadoraServidor.Services;

namespace PanificadoraServidor.Endpoints;

/// <summary>API do site da distribuidora: cadastros, pedidos (corte/faturamento) e contas a receber.</summary>
public static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app)
    {
        var g = app.MapGroup("/api/admin");

        Crud<Estado>(g, "estados");
        Crud<Cidade>(g, "cidades");
        Crud<GrupoProduto>(g, "grupos");
        Crud<Produto>(g, "produtos");
        Crud<Vendedor>(g, "vendedores");
        Crud<FormaPagamento>(g, "formas");
        Crud<Cliente>(g, "clientes", ValidarCliente);
        Crud<VendedorPraca>(g, "pracas", ValidarPraca);
        Crud<RoteiroItem>(g, "roteiro", ValidarRoteiro);
        Condicoes(g);
        Pedidos(g);
        Financeiro(g);
    }

    // ---------- CRUD genérico (excluir = inativar, quando a entidade tem status) ----------
    static void Crud<T>(RouteGroupBuilder g, string rota, Func<AppDb, T, Task<string?>>? validar = null) where T : class, IEntity
    {
        g.MapGet($"/{rota}", async (AppDb db) => await db.Set<T>().AsNoTracking().OrderBy(x => EF.Property<int>(x, "Id")).ToListAsync());

        g.MapPost($"/{rota}", async Task<IResult> (T e, AppDb db) =>
        {
            e.Id = 0;
            var erro = validar == null ? null : await validar(db, e);
            if (erro != null) return Results.BadRequest(new { erro });
            db.Add(e);
            await db.SaveChangesAsync();
            return Results.Ok(e);
        });

        g.MapPut($"/{rota}/{{id:int}}", async Task<IResult> (int id, T e, AppDb db) =>
        {
            e.Id = id;
            var erro = validar == null ? null : await validar(db, e);
            if (erro != null) return Results.BadRequest(new { erro });
            db.Update(e);
            await db.SaveChangesAsync();
            return Results.Ok(e);
        });

        g.MapDelete($"/{rota}/{{id:int}}", async Task<IResult> (int id, AppDb db) =>
        {
            var e = await db.Set<T>().FindAsync(id);
            if (e == null) return Results.NotFound();
            if (e is IAtivo a) a.Ativo = false; else db.Remove(e);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    static async Task<string?> ValidarCliente(AppDb db, Cliente c)
    {
        c.CpfCnpj = new string(c.CpfCnpj.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(c.RazaoSocial)) return "Informe a razão social.";
        if (c.CpfCnpj.Length != 11 && c.CpfCnpj.Length != 14) return "CPF (MEI) ou CNPJ deve ter 11 ou 14 dígitos.";
        if (c.CidadeId == 0) return "Informe a cidade.";
        if (await db.Clientes.AnyAsync(x => x.CpfCnpj == c.CpfCnpj && x.Id != c.Id)) return "Já existe cliente com este CPF/CNPJ.";
        return null;
    }

    /// <summary>Regra: não pode haver dois vendedores ativos concorrendo na mesma praça.</summary>
    public static async Task<Vendedor?> DonoDaPracaAsync(AppDb db, int cidadeId, int ignorarPracaId = 0)
    {
        var vendIds = await db.Pracas.Where(p => p.CidadeId == cidadeId && p.Id != ignorarPracaId).Select(p => p.VendedorId).ToListAsync();
        return await db.Vendedores.FirstOrDefaultAsync(v => v.Ativo && vendIds.Contains(v.Id));
    }

    static async Task<string?> ValidarPraca(AppDb db, VendedorPraca p)
    {
        if (!await db.Vendedores.AnyAsync(v => v.Id == p.VendedorId && v.Ativo)) return "Vendedor inválido ou inativo.";
        var cidade = await db.Cidades.FindAsync(p.CidadeId);
        if (cidade == null) return "Cidade inválida.";
        var dono = await DonoDaPracaAsync(db, p.CidadeId, p.Id);
        if (dono != null && dono.Id != p.VendedorId) return $"A praça {cidade.Nome} já pertence ao vendedor {dono.Nome}. Não é permitido dois vendedores concorrendo na mesma praça.";
        if (await db.Pracas.AnyAsync(x => x.VendedorId == p.VendedorId && x.CidadeId == p.CidadeId && x.Id != p.Id)) return "Este vendedor já atende esta praça.";
        return null;
    }

    static async Task<string?> ValidarRoteiro(AppDb db, RoteiroItem r)
    {
        var cli = await db.Clientes.FindAsync(r.ClienteId);
        if (cli == null) return "Cliente inválido.";
        if (r.DiaSemana is < 0 or > 6) return "Dia da semana inválido.";
        if (r.Sequencia <= 0) return "Sequência deve ser maior que zero (use 10, 20, 30... para poder encaixar clientes no meio).";
        var dono = await DonoDaPracaAsync(db, cli.CidadeId);
        if (dono == null || dono.Id != r.VendedorId) return "O vendedor não atende a praça (cidade) deste cliente. Cadastre a praça primeiro.";
        if (await db.Roteiro.AnyAsync(x => x.ClienteId == r.ClienteId && x.Id != r.Id)) return "Este cliente já está no roteiro de um vendedor.";
        return null;
    }

    // ---------- Condições de pagamento (com parcelas) ----------
    static void Condicoes(RouteGroupBuilder g)
    {
        g.MapGet("/condicoes", async (AppDb db) => await db.Condicoes.Include(c => c.Parcelas).AsNoTracking().OrderBy(c => EF.Property<int>(c, "Id")).ToListAsync());

        static string? Validar(CondicaoPagamento c)
        {
            if (string.IsNullOrWhiteSpace(c.Descricao)) return "Informe a descrição.";
            if (c.Parcelas.Count == 0) return "Informe ao menos uma parcela.";
            if (Math.Abs(c.Parcelas.Sum(p => p.Percentual) - 100m) > 0.02m) return "A soma dos percentuais das parcelas deve ser 100%.";
            return null;
        }

        g.MapPost("/condicoes", async Task<IResult> (CondicaoPagamento c, AppDb db) =>
        {
            var erro = Validar(c);
            if (erro != null) return Results.BadRequest(new { erro });
            c.Id = 0;
            foreach (var p in c.Parcelas) { p.Id = 0; p.CondicaoPagamentoId = 0; }
            db.Condicoes.Add(c);
            await db.SaveChangesAsync();
            return Results.Ok(c);
        });

        g.MapPut("/condicoes/{id:int}", async Task<IResult> (int id, CondicaoPagamento c, AppDb db) =>
        {
            var erro = Validar(c);
            if (erro != null) return Results.BadRequest(new { erro });
            var atual = await db.Condicoes.Include(x => x.Parcelas).FirstOrDefaultAsync(x => x.Id == id);
            if (atual == null) return Results.NotFound();
            atual.Descricao = c.Descricao; atual.MultaPercentual = c.MultaPercentual; atual.JurosPercentual = c.JurosPercentual;
            atual.DescontoPercentual = c.DescontoPercentual; atual.Ativo = c.Ativo;
            db.Parcelas.RemoveRange(atual.Parcelas);
            atual.Parcelas = c.Parcelas.Select(p => new CondicaoParcela { Numero = p.Numero, Dias = p.Dias, Percentual = p.Percentual, FormaPagamentoId = p.FormaPagamentoId }).ToList();
            await db.SaveChangesAsync();
            return Results.Ok(atual);
        });

        g.MapDelete("/condicoes/{id:int}", async Task<IResult> (int id, AppDb db) =>
        {
            var c = await db.Condicoes.FindAsync(id);
            if (c == null) return Results.NotFound();
            c.Ativo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // ---------- Pedidos recebidos: corte e faturamento ----------
    static void Pedidos(RouteGroupBuilder g)
    {
        g.MapGet("/pedidos", async (string? status, AppDb db) =>
        {
            status ??= "Recebido";
            var peds = await db.Pedidos.Include(p => p.Itens).Where(p => p.Status == status).ToListAsync();
            var clientes = await db.Clientes.ToDictionaryAsync(c => c.Id);
            var vend = await db.Vendedores.ToDictionaryAsync(v => v.Id);
            var cond = await db.Condicoes.ToDictionaryAsync(c => c.Id);
            var prods = await db.Produtos.ToDictionaryAsync(p => p.Id);
            var deve = (await db.Contas.Where(c => !c.Pago).ToListAsync()).GroupBy(c => c.ClienteId).ToDictionary(x => x.Key, x => x.Sum(c => c.Valor));

            return peds.OrderBy(p => p.DataHora).Select(p => new
            {
                p.Id, p.DataHora, p.Status, p.VendaId,
                cliente = clientes.GetValueOrDefault(p.ClienteId)?.NomeFantasia is { Length: > 0 } n ? n : clientes.GetValueOrDefault(p.ClienteId)?.RazaoSocial,
                vendedor = vend.GetValueOrDefault(p.VendedorId)?.Nome,
                condicao = cond.GetValueOrDefault(p.CondicaoPagamentoId)?.Descricao,
                clienteDeve = deve.GetValueOrDefault(p.ClienteId),
                itens = p.Itens.Select(i => new
                {
                    i.Id, i.ProdutoId, i.Descricao, i.QtdPedida, i.QtdLiberada, i.PrecoUnitario,
                    estoque = prods.GetValueOrDefault(i.ProdutoId)?.Estoque ?? 0
                })
            });
        });

        g.MapPost("/pedidos/corte", async (AppDb db) => Results.Ok(new { itensCortados = await VendaService.AplicarCorteAsync(db) }));

        g.MapPut("/pedidos/{id:int}/liberado", async Task<IResult> (int id, List<LiberadoDto> lista, AppDb db) =>
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
            if (ped == null) return Results.NotFound();
            if (ped.Status != "Recebido") return Results.BadRequest(new { erro = "Pedido já processado." });
            foreach (var l in lista)
            {
                var item = ped.Itens.FirstOrDefault(i => i.Id == l.ItemId);
                if (item == null) continue;
                if (l.QtdLiberada < 0 || l.QtdLiberada > item.QtdPedida) return Results.BadRequest(new { erro = $"Quantidade liberada de '{item.Descricao}' deve ficar entre 0 e {item.QtdPedida}." });
                item.QtdLiberada = l.QtdLiberada;
            }
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/pedidos/{id:int}/faturar", async Task<IResult> (int id, AppDb db) =>
        {
            var ped = await db.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
            if (ped == null) return Results.NotFound();
            try { var v = await VendaService.FaturarAsync(db, ped); return Results.Ok(new { venda = v?.Numero, total = v?.Total }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { erro = ex.Message }); }
        });

        g.MapPost("/pedidos/faturar-todos", async (AppDb db) =>
        {
            var ids = await db.Pedidos.Where(p => p.Status == "Recebido").OrderBy(p => p.DataHora).Select(p => p.Id).ToListAsync();
            int ok = 0; var erros = new List<string>();
            foreach (var id in ids)
            {
                var ped = await db.Pedidos.Include(p => p.Itens).FirstAsync(p => p.Id == id);
                try { await VendaService.FaturarAsync(db, ped); ok++; }
                catch (InvalidOperationException ex) { erros.Add($"Pedido {id}: {ex.Message}"); db.ChangeTracker.Clear(); }
            }
            return Results.Ok(new { faturados = ok, erros });
        });

        g.MapPost("/pedidos/{id:int}/cancelar", async Task<IResult> (int id, AppDb db) =>
        {
            var ped = await db.Pedidos.FindAsync(id);
            if (ped == null) return Results.NotFound();
            if (ped.Status != "Recebido") return Results.BadRequest(new { erro = "Só é possível cancelar pedido recebido." });
            ped.Status = "Cancelado";
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    // ---------- Vendas e contas a receber ----------
    static void Financeiro(RouteGroupBuilder g)
    {
        g.MapGet("/vendas", async (AppDb db) =>
        {
            var clientes = await db.Clientes.ToDictionaryAsync(c => c.Id);
            var vendas = await db.Vendas.ToListAsync();
            return vendas.OrderByDescending(v => v.DataVenda).Select(v => new
            {
                v.Id, v.Numero, v.DataVenda, v.Total, v.PedidoId,
                cliente = clientes.GetValueOrDefault(v.ClienteId)?.RazaoSocial
            });
        });

        g.MapGet("/contas", async (string? status, AppDb db) =>
        {
            var q = db.Contas.AsQueryable();
            if (status == "aberto") q = q.Where(c => !c.Pago);
            else if (status == "pago") q = q.Where(c => c.Pago);
            var lista = await q.ToListAsync();
            var clientes = await db.Clientes.ToDictionaryAsync(c => c.Id);
            var formas = await db.Formas.ToDictionaryAsync(f => f.Id);
            return lista.OrderBy(c => c.Vencimento).Select(c => new
            {
                c.Id, c.Documento, c.Vencimento, c.Valor, c.Pago, c.DataPagamento, c.ValorPago,
                cliente = clientes.GetValueOrDefault(c.ClienteId)?.RazaoSocial,
                forma = formas.GetValueOrDefault(c.FormaPagamentoId)?.Descricao,
                atrasada = !c.Pago && c.Vencimento.Date < DateTime.Today
            });
        });

        g.MapGet("/contas/{id:int}/calcular", async Task<IResult> (int id, DateTime? data, AppDb db) =>
        {
            var c = await db.Contas.FindAsync(id);
            if (c == null) return Results.NotFound();
            var r = VendaService.Calcular(c, data ?? DateTime.Today);
            return Results.Ok(new { c.Valor, r.Multa, r.Juros, r.Desconto, r.Total });
        });

        g.MapPost("/contas/{id:int}/baixar", async Task<IResult> (int id, BaixaDto dto, AppDb db) =>
        {
            var c = await db.Contas.FindAsync(id);
            if (c == null) return Results.NotFound();
            if (c.Pago) return Results.BadRequest(new { erro = "Conta já está paga." });
            var data = dto.Data ?? DateTime.Today;
            c.ValorPago = VendaService.Calcular(c, data).Total;
            c.Pago = true; c.DataPagamento = data;
            await db.SaveChangesAsync();
            return Results.Ok(new { c.ValorPago });
        });
    }
}
