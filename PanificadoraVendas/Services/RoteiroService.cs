using PanificadoraVendas.Models;

namespace PanificadoraVendas.Services;

public static class RoteiroService
{
    public static List<Cliente> RotaDoDia(IEnumerable<Cliente> todos, int dia) =>
        todos.Where(c => c.Ativo && c.DiaVisita == dia).OrderBy(c => c.Sequencia).ThenBy(c => c.Nome).ToList();

    public static void Renumerar(List<Cliente> rota)
    {
        int s = 10;
        foreach (var c in rota) { c.Sequencia = s; s += 10; }
    }

    /// <summary>Calcula a sequência de um cliente novo (ex.: entre 10 e 20 vira 15).</summary>
    public static int CalcularSequencia(List<Cliente> rota, int? aposClienteId, bool inicio)
    {
        if (rota.Count == 0) return 10;

        if (inicio)
        {
            if (rota[0].Sequencia >= 2) return rota[0].Sequencia / 2;
            Renumerar(rota);
            return rota[0].Sequencia / 2;
        }

        if (aposClienteId == null) return rota[^1].Sequencia + 10;

        int idx = rota.FindIndex(c => c.Id == aposClienteId);
        if (idx < 0 || idx == rota.Count - 1) return rota[^1].Sequencia + 10;

        int prev = rota[idx].Sequencia, next = rota[idx + 1].Sequencia, meio = (prev + next) / 2;
        if (meio > prev && meio < next) return meio;

        Renumerar(rota);                       // sem brecha: reorganiza em 10, 20, 30...
        return (rota[idx].Sequencia + rota[idx + 1].Sequencia) / 2;
    }
}

public static class FinanceiroService
{
    /// <summary>Prévia do contas a receber gerado pela condição de pagamento (a última parcela absorve o arredondamento).</summary>
    public static List<(int Numero, DateTime Vencimento, decimal Valor, string Forma)> GerarParcelas(decimal total, CondicaoPagamento c, DateTime dataBase)
    {
        var lista = new List<(int, DateTime, decimal, string)>();
        decimal acumulado = 0;
        var parcelas = c.Parcelas.OrderBy(p => p.Numero).ToList();
        for (int i = 0; i < parcelas.Count; i++)
        {
            var p = parcelas[i];
            decimal valor = i == parcelas.Count - 1 ? total - acumulado : Math.Round(total * p.Percentual / 100m, 2);
            acumulado += valor;
            lista.Add((p.Numero, dataBase.Date.AddDays(p.Dias), valor, p.FormaPagamento));
        }
        return lista;
    }
}
