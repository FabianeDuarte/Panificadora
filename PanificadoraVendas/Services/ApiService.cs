using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PanificadoraVendas.Models;

namespace PanificadoraVendas.Services;

/// <summary>
/// Sincronização manual (o vendedor decide quando baixar e quando enviar). Todas as chamadas levam o token do login.
/// GET  {ApiUrl}/api/app/dados    -> SyncDto   (o vendedor é o do token)
/// POST {ApiUrl}/api/app/pedidos  <- EnvioDto
/// Sem ApiUrl configurada o app funciona em MODO DEMO.
/// </summary>
public class ApiService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    const string SessaoExpirada = "Sessão expirada ou sem login online. Vá em Sincronizar > Sair, e entre novamente com internet.";
    readonly LocalStore _store;
    readonly AuthService _auth;

    public ApiService(LocalStore store, AuthService auth) { _store = store; _auth = auth; }

    async Task<HttpRequestMessage?> CriarAsync(HttpMethod metodo, string caminho, object? corpo = null)
    {
        var token = await _auth.ObterTokenAsync();
        if (string.IsNullOrEmpty(token)) return null;
        var req = new HttpRequestMessage(metodo, $"{_store.ApiUrl.TrimEnd('/')}{caminho}");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (corpo != null) req.Content = JsonContent.Create(corpo, options: Json);
        return req;
    }

    public async Task<(bool Ok, string Msg)> BaixarAsync()
    {
        try
        {
            SyncDto dto;
            if (_store.ModoDemo)
            {
                await Task.Delay(600);
                var s = SeedData.Criar();
                dto = new SyncDto { Clientes = s.Clientes, Produtos = s.Produtos, UltimasCompras = s.UltimasCompras, Condicoes = s.Condicoes };
            }
            else
            {
                using var req = await CriarAsync(HttpMethod.Get, "/api/app/dados");
                if (req == null) return (false, SessaoExpirada);
                using var resp = await Http.SendAsync(req);
                if (resp.StatusCode == HttpStatusCode.Unauthorized) return (false, SessaoExpirada);
                resp.EnsureSuccessStatusCode();
                dto = await resp.Content.ReadFromJsonAsync<SyncDto>(Json) ?? throw new Exception("Resposta vazia da API.");
            }

            var d = _store.Dados;
            var locais = d.Clientes.Where(c => c.Novo && (!c.EnviadoServidor || _store.ModoDemo)).ToList();
            d.Clientes = dto.Clientes.Where(c => c.Ativo).ToList();
            d.Clientes.AddRange(locais.Where(l => d.Clientes.All(c => c.Id != l.Id)));
            d.Produtos = dto.Produtos.Where(p => p.Ativo).ToList();
            d.UltimasCompras = dto.UltimasCompras;
            d.Condicoes = dto.Condicoes.Where(c => c.Ativo).ToList();
            d.UltimaSincronizacao = DateTime.Now;
            await _store.SalvarAsync();
            return (true, $"Baixados {d.Clientes.Count} clientes, {d.Produtos.Count} produtos e {d.Condicoes.Count} condições de pagamento.");
        }
        catch (Exception ex) { return (false, $"Não foi possível baixar: {ex.Message}"); }
    }

    public async Task<(bool Ok, string Msg)> EnviarAsync()
    {
        var d = _store.Dados;
        var pend = d.Pedidos.Where(p => !p.Enviado).ToList();
        var novos = d.Clientes.Where(c => c.Novo && !c.EnviadoServidor).ToList();
        if (pend.Count == 0 && novos.Count == 0) return (true, "Nada pendente para enviar.");
        try
        {
            if (_store.ModoDemo) await Task.Delay(800);
            else
            {
                using var req = await CriarAsync(HttpMethod.Post, "/api/app/pedidos",
                    new EnvioDto { VendedorId = _store.VendedorId, ClientesNovos = novos, Pedidos = pend });
                if (req == null) return (false, SessaoExpirada + " Os pedidos continuam salvos no aparelho.");
                using var resp = await Http.SendAsync(req);
                if (resp.StatusCode == HttpStatusCode.Unauthorized) return (false, SessaoExpirada + " Os pedidos continuam salvos no aparelho.");
                resp.EnsureSuccessStatusCode();
            }
            foreach (var p in pend) { p.Enviado = true; p.DataEnvio = DateTime.Now; }
            foreach (var c in novos) c.EnviadoServidor = true;
            await _store.SalvarAsync();
            return (true, $"Enviados {pend.Count} pedido(s) e {novos.Count} cliente(s) novo(s).");
        }
        catch (Exception ex)
        {
            return (false, $"Falha no envio. Os pedidos continuam salvos no aparelho. ({ex.Message})");
        }
    }
}
