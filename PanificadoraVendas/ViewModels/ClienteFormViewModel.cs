using System.Collections.ObjectModel;
using System.Windows.Input;
using PanificadoraVendas.Models;
using PanificadoraVendas.Services;

namespace PanificadoraVendas.ViewModels;

/// <summary>Cadastro de cliente novo feito pelo vendedor na rua (ex.: apareceu uma panificadora nova).</summary>
public class ClienteFormViewModel : BaseViewModel
{
    readonly LocalStore _store;
    int _diaIndex = (int)DateTime.Today.DayOfWeek;
    PosicaoOpcao? _posicao;

    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "PR";

    public string[] Dias { get; } = { "Domingo", "Segunda-feira", "Terça-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sábado" };
    public ObservableCollection<PosicaoOpcao> Posicoes { get; } = new();
    public ICommand SalvarCommand { get; }

    public int DiaIndex
    {
        get => _diaIndex;
        set { if (value < 0) return; if (SetProperty(ref _diaIndex, value)) CarregarPosicoes(); }
    }

    public PosicaoOpcao? Posicao { get => _posicao; set => SetProperty(ref _posicao, value); }

    public ClienteFormViewModel(LocalStore store)
    {
        _store = store;
        SalvarCommand = new Command(async () => await SalvarAsync());
        CarregarPosicoes();
    }

    void CarregarPosicoes()
    {
        Posicoes.Clear();
        Posicoes.Add(new PosicaoOpcao { Texto = "No final da rota" });
        Posicoes.Add(new PosicaoOpcao { Texto = "No início da rota", Inicio = true });
        foreach (var c in RoteiroService.RotaDoDia(_store.Dados.Clientes, _diaIndex))
            Posicoes.Add(new PosicaoOpcao { Texto = $"Depois de {c.Sequencia} - {c.Nome}", AposClienteId = c.Id });
        Posicao = Posicoes[0];
    }

    async Task SalvarAsync()
    {
        var doc = new string((CpfCnpj ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(RazaoSocial) || string.IsNullOrWhiteSpace(Cidade))
        { await Shell.Current.DisplayAlertAsync("Cliente", "Informe a razão social e a cidade.", "OK"); return; }
        if (doc.Length != 11 && doc.Length != 14)
        { await Shell.Current.DisplayAlertAsync("Cliente", "Informe um CPF (MEI) ou CNPJ válido com 11 ou 14 dígitos.", "OK"); return; }
        if (_store.Dados.Clientes.Any(c => c.CpfCnpj == doc))
        { await Shell.Current.DisplayAlertAsync("Cliente", "Já existe um cliente com este CPF/CNPJ.", "OK"); return; }

        var rota = RoteiroService.RotaDoDia(_store.Dados.Clientes, _diaIndex);
        int seq = RoteiroService.CalcularSequencia(rota, Posicao?.AposClienteId, Posicao?.Inicio ?? false);

        _store.Dados.Clientes.Add(new Cliente
        {
            Id = _store.ProximoIdLocal(), RazaoSocial = RazaoSocial.Trim(), NomeFantasia = (NomeFantasia ?? "").Trim(),
            CpfCnpj = doc, Telefone = Telefone ?? "", Endereco = Endereco ?? "", Bairro = Bairro ?? "",
            Cidade = Cidade.Trim(), Uf = (Uf ?? "PR").ToUpperInvariant(), DiaVisita = _diaIndex, Sequencia = seq,
            Novo = true, Ativo = true, SaldoDevedor = 0
        });
        await _store.SalvarAsync();
        await Shell.Current.DisplayAlertAsync("Cliente", $"Cliente cadastrado na sequência {seq}. Será enviado junto com os pedidos.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}
