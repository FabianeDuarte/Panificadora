using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class SincronizarPage : ContentPage
{
    readonly SincronizarViewModel _vm;
    public SincronizarPage(SincronizarViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.AtualizarInfo(); }
}
