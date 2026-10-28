using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace PanificadoraVendas.Services;

public class LoginResposta
{
    public string Token { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Perfil { get; set; } = "";
    public int? VendedorId { get; set; }
    public DateTime Expira { get; set; }
}

/// <summary>
/// Login do vendedor. Online: valida no servidor e guarda o token (SecureStorage).
/// Sem internet: entra com usuário/senha já usados neste aparelho (hash PBKDF2 guardado no SecureStorage).
/// </summary>
public class AuthService
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    readonly LocalStore _store;
    string _token = "";

    public AuthService(LocalStore store) => _store = store;

    public bool Logado => Preferences.Get("sess_logado", false);
    public string Usuario => Preferences.Get("sess_login", "");
    public string NomeVendedor => Preferences.Get("sess_nome", "");
    public bool Offline => Preferences.Get("sess_offline", false);
    public string UltimoLogin => Preferences.Get("ultimo_login", "");

    public async Task<string> ObterTokenAsync()
    {
        if (!string.IsNullOrEmpty(_token)) return _token;
        try { _token = await SecureStorage.Default.GetAsync("sess_token") ?? ""; } catch { }
        return _token;
    }

    public void Sair()
    {
        Preferences.Set("sess_logado", false);
        _token = "";
        try { SecureStorage.Default.Remove("sess_token"); } catch { }
    }

    public async Task<(bool Ok, string Msg)> EntrarAsync(string login, string senha, string apiUrl)
    {
        login = (login ?? "").Trim();
        if (login.Length == 0 || string.IsNullOrEmpty(senha)) return (false, "Informe usuário e senha.");
        _store.ApiUrl = (apiUrl ?? "").Trim();

        // ----- modo demonstração (sem servidor) -----
        if (_store.ModoDemo)
        {
            if (!login.Equals("demo", StringComparison.OrdinalIgnoreCase) || senha != "demo")
                return (false, "Sem servidor configurado, use o usuário demo e a senha demo (modo demonstração).");
            if (_store.Dados.VendedorId != 0)
                return (false, "Este aparelho tem dados de um vendedor real. Informe a URL do servidor para entrar.");
            await IniciarAsync("demo", "Vendedor Demo", 0, "", offline: false);
            return (true, "");
        }

        // ----- online -----
        try
        {
            var resp = await Http.PostAsJsonAsync($"{_store.ApiUrl.TrimEnd('/')}/api/auth/login", new { login, senha }, Json);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or (HttpStatusCode)423 or HttpStatusCode.BadRequest)
                    return (false, await LerErroAsync(resp));
                return (false, $"Servidor respondeu {(int)resp.StatusCode}. Tente novamente.");
            }

            var r = await resp.Content.ReadFromJsonAsync<LoginResposta>(Json);
            if (r == null || string.IsNullOrEmpty(r.Token)) return (false, "Resposta inválida do servidor.");
            if (r.Perfil != "Vendedor" || r.VendedorId == null) return (false, "Este usuário não é vendedor. O acesso de administrador é pelo site.");

            var bloqueio = ValidarTroca(r.VendedorId.Value);
            if (bloqueio != null) return (false, bloqueio);
            await AplicarTrocaAsync(r.VendedorId.Value);

            await GuardarOfflineAsync(login, senha, r.VendedorId.Value, r.Nome);
            await IniciarAsync(login, r.Nome, r.VendedorId.Value, r.Token, offline: false);
            return (true, "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // sem sinal: tenta entrar offline com credenciais já usadas neste aparelho
            var off = await ConferirOfflineAsync(login, senha);
            if (off == null)
                return (false, "Sem conexão com o servidor. Só é possível entrar sem internet com um usuário que já entrou neste aparelho.");

            var bloqueio = ValidarTroca(off.Value.VendedorId);
            if (bloqueio != null) return (false, bloqueio);
            await IniciarAsync(login, off.Value.Nome, off.Value.VendedorId, "", offline: true);
            return (true, "Sem conexão: você entrou em modo offline. Para sincronizar, entre de novo com internet.");
        }
        catch (Exception ex) { return (false, $"Erro ao entrar: {ex.Message}"); }
    }

    static async Task<string> LerErroAsync(HttpResponseMessage resp)
    {
        try
        {
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("erro", out var e)) return e.GetString() ?? "Falha no login.";
        }
        catch { }
        return "Falha no login.";
    }

    async Task IniciarAsync(string login, string nome, int vendedorId, string token, bool offline)
    {
        _token = token;
        try
        {
            if (string.IsNullOrEmpty(token)) SecureStorage.Default.Remove("sess_token");
            else await SecureStorage.Default.SetAsync("sess_token", token);
        }
        catch { /* alguns dispositivos não têm cofre: o token fica só na memória */ }

        Preferences.Set("sess_login", login);
        Preferences.Set("sess_nome", nome);
        Preferences.Set("sess_offline", offline);
        Preferences.Set("ultimo_login", login);
        _store.VendedorId = vendedorId;
        _store.VendedorNome = nome;
        Preferences.Set("sess_logado", true);
    }

    // ----- um aparelho, vários vendedores: nunca misturar os dados -----
    string? ValidarTroca(int vendedorId)
    {
        var d = _store.Dados;
        if (d.VendedorId == vendedorId) return null;
        bool pendente = d.Pedidos.Any(p => !p.Enviado) || d.Clientes.Any(c => c.Novo && !c.EnviadoServidor);
        if (d.VendedorId != 0 && pendente)
            return "Há pedidos ou clientes pendentes de outro vendedor neste aparelho. Ele precisa entrar e enviar antes da troca de usuário.";
        return null;
    }

    async Task AplicarTrocaAsync(int vendedorId)
    {
        var d = _store.Dados;
        if (d.VendedorId == vendedorId) return;
        d.Clientes.Clear(); d.Produtos.Clear(); d.UltimasCompras.Clear(); d.Condicoes.Clear();
        d.Pedidos.Clear(); d.Visitas.Clear();
        d.UltimaSincronizacao = null;
        d.VendedorId = vendedorId;
        await _store.SalvarAsync();
    }

    // ----- login offline -----
    static async Task GuardarOfflineAsync(string login, string senha, int vendedorId, string nome)
    {
        try
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var h = Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, HashAlgorithmName.SHA256, 32);
            var dados = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(h)}:{vendedorId}:{Uri.EscapeDataString(nome)}";
            await SecureStorage.Default.SetAsync("off_" + login.ToLowerInvariant(), dados);
        }
        catch { }
    }

    static async Task<(int VendedorId, string Nome)?> ConferirOfflineAsync(string login, string senha)
    {
        try
        {
            var d = await SecureStorage.Default.GetAsync("off_" + login.ToLowerInvariant());
            if (d == null) return null;
            var p = d.Split(':');
            if (p.Length != 4) return null;
            var esperado = Convert.FromBase64String(p[1]);
            var h = Rfc2898DeriveBytes.Pbkdf2(senha, Convert.FromBase64String(p[0]), 100_000, HashAlgorithmName.SHA256, esperado.Length);
            if (!CryptographicOperations.FixedTimeEquals(h, esperado)) return null;
            return (int.Parse(p[2]), Uri.UnescapeDataString(p[3]));
        }
        catch { return null; }
    }
}
