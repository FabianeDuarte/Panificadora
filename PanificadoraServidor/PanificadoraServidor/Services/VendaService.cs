using Microsoft.EntityFrameworkCore;
using PanificadoraServidor.Data;
using PanificadoraServidor.Models;

namespace PanificadoraServidor.Services;

public static class VendaService
{
    /// <summary>
    /// CORTE: para cada produto cuja soma dos pedidos passa do estoque, distribui o estoque
    /// proporcionalmente ao pedido. A sobra de arredondamento vai primeiro para clientes sem débito.
    /// O resultado é só uma sugestão: o gestor pode ajustar a quantidade liberada manualmente.
    /// </summary>
    public static async Task<int> AplicarCorteAsync(AppDb db)
    {
        var pedidos = await db.Pedidos.Include(p => p.Itens).Where(p => p.Status == "Recebido").ToListAsync();
        var produtos = await db.Produtos.ToDictionaryAsync(p => p.Id);
        var devedores = (await db.Contas.Where(c => !c.Pago).ToListAsync()).Select(c => c.ClienteId).ToHashSet();
        int cortados = 0;

        var porProduto = pedidos.SelectMany(p => p.Itens.Select(i => (Pedido: p, Item: i))).GroupBy(x => x.Item.ProdutoId);
        foreach (var g in porProduto)
        {
            int estoque = Math.Max(0, produtos.TryGetValue(g.Key, out var prod) ? prod.Estoque : 0);
            var lista = g.ToList();
            int demanda = lista.Sum(x => x.Item.QtdPedida);

            if (demanda <= estoque) { foreach (var x in lista) x.Item.QtdLiberada = x.Item.QtdPedida; continue; }

            int alocado = 0;
            foreach (var x in lista)
            {
                x.Item.QtdLiberada = (int)Math.Floor((double)estoque * x.Item.QtdPedida / demanda);
                alocado += x.Item.QtdLiberada;
            }
            int resto = estoque - alocado;
            var ordem = lista.OrderBy(x => devedores.Contains(x.Pedido.ClienteId) ? 1 : 0)
                             .ThenByDescending(x => x.Item.QtdPedida - x.Item.QtdLiberada).ToList();
            while (resto > 0 && ordem.Any(x => x.Item.QtdLiberada < x.Item.QtdPedida))
                foreach (var x in ordem)
                {
                    if (resto <= 0) break;
                    if (x.Item.QtdLiberada < x.Item.QtdPedida) { x.Item.QtdLiberada++; resto--; }
                }
            cortados += lista.Count(x => x.Item.QtdLiberada < x.Item.QtdPedida);
        }
        await db.SaveChangesAsync();
        return cortados;
    }

    /// <summary>Transforma o pedido em venda: baixa estoque (nunca negativo) e gera o contas a receber.</summary>
    public static async Task<Venda?> FaturarAsync(AppDb db, Pedido pedido)
    {
        if (pedido.Status != "Recebido") throw new InvalidOperationException($"Pedido {pedido.Id} já está {pedido.Status}.");

        var itens = pedido.Itens.Where(i => i.QtdLiberada > 0).ToList();
        if (itens.Count == 0) { pedido.Status = "Cancelado"; await db.SaveChangesAsync(); return null; }

        var cond = await db.Condicoes.Include(c => c.Parcelas).FirstOrDefaultAsync(c => c.Id == pedido.CondicaoPagamentoId)
                   ?? throw new InvalidOperationException("Condição de pagamento do pedido não encontrada.");

        var pids = itens.Select(i => i.ProdutoId).ToList();
        var produtos = await db.Produtos.Where(p => pids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        foreach (var i in itens)
        {
            var p = produtos[i.ProdutoId];
            if (p.Estoque < i.QtdLiberada)
                throw new InvalidOperationException($"Estoque insuficiente de '{p.Descricao}': disponível {p.Estoque}, liberado {i.QtdLiberada}.");
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var i in itens) produtos[i.ProdutoId].Estoque -= i.QtdLiberada;

        var numeros = await db.Vendas.Select(v => v.Numero).ToListAsync();
        var venda = new Venda
        {
            Numero = (numeros.Count == 0 ? 100 : numeros.Max()) + 1,
            PedidoId = pedido.Id, ClienteId = pedido.ClienteId, CondicaoPagamentoId = cond.Id,
            DataVenda = DateTime.Now, Total = itens.Sum(i => i.QtdLiberada * i.PrecoUnitario)
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        foreach (var i in itens)
            db.VendaItens.Add(new VendaItem { VendaId = venda.Id, ProdutoId = i.ProdutoId, Quantidade = i.QtdLiberada, PrecoUnitario = i.PrecoUnitario });

        decimal acumulado = 0;
        var parcelas = cond.Parcelas.OrderBy(p => p.Numero).ToList();
        for (int k = 0; k < parcelas.Count; k++)
        {
            var p = parcelas[k];
            decimal valor = k == parcelas.Count - 1 ? venda.Total - acumulado : Math.Round(venda.Total * p.Percentual / 100m, 2);
            acumulado += valor;
            db.Contas.Add(new ContaReceber
            {
                VendaId = venda.Id, ClienteId = venda.ClienteId, Documento = $"{venda.Numero}/{p.Numero}", Parcela = p.Numero,
                Vencimento = venda.DataVenda.Date.AddDays(p.Dias), Valor = valor, FormaPagamentoId = p.FormaPagamentoId,
                MultaPct = cond.MultaPercentual, JurosPct = cond.JurosPercentual, DescontoPct = cond.DescontoPercentual
            });
        }

        pedido.Status = "Faturado";
        pedido.VendaId = venda.Id;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return venda;
    }

    /// <summary>Multa e juros se pagar atrasado; desconto se pagar antes do vencimento.</summary>
    public static (decimal Multa, decimal Juros, decimal Desconto, decimal Total) Calcular(ContaReceber c, DateTime pagamento)
    {
        decimal multa = 0, juros = 0, desconto = 0;
        int dias = (pagamento.Date - c.Vencimento.Date).Days;
        if (dias > 0)
        {
            multa = Math.Round(c.Valor * c.MultaPct / 100m, 2);
            juros = Math.Round(c.Valor * c.JurosPct / 100m / 30m * dias, 2);   // juros % ao mês, proporcional aos dias
        }
        else if (dias < 0)
        {
            decimal pct = Math.Min(100m, c.DescontoPct * -dias);                // desconto % por dia de antecipação
            desconto = Math.Round(c.Valor * pct / 100m, 2);
        }
        return (multa, juros, desconto, c.Valor + multa + juros - desconto);
    }
}
