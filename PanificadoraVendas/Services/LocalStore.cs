using System.Text.Json;
using PanificadoraVendas.Models;

namespace PanificadoraVendas.Services;

/// <summary>Armazenamento offline (JSON no aparelho). O vendedor trabalha sem internet.</summary>
public class LocalStore
{
    readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "dados.json");
    readonly SemaphoreSlim _lock = new(1, 1);

    public DadosLocais Dados { get; }

    public LocalStore() => Dados = Carregar();

    DadosLocais Carregar()
    {
        try
        {
            if (File.Exists(_path))
            {
                var d = JsonSerializer.Deserialize<DadosLocais>(File.ReadAllText(_path));
                if (d != null) return d;
            }
        }
        catch { /* arquivo corrompido: recria */ }
        var seed = SeedData.Criar();
        try { File.WriteAllText(_path, JsonSerializer.Serialize(seed)); } catch { }
        return seed;
    }

    public async Task SalvarAsync()
    {
        await _lock.WaitAsync();
        try { await File.WriteAllTextAsync(_path, JsonSerializer.Serialize(Dados)); }
        finally { _lock.Release(); }
    }

    public string ApiUrl { get => Preferences.Get("api_url", ""); set => Preferences.Set("api_url", value ?? ""); }
    public int VendedorId { get => Preferences.Get("vendedor_id", 1); set => Preferences.Set("vendedor_id", value); }
    public string VendedorNome { get => Preferences.Get("vendedor_nome", "Vendedor"); set => Preferences.Set("vendedor_nome", value ?? ""); }
    public bool ModoDemo => string.IsNullOrWhiteSpace(ApiUrl);

    public int ProximoIdLocal() => Math.Min(-1, Dados.Clientes.Select(c => c.Id).DefaultIfEmpty(0).Min() - 1);
}
