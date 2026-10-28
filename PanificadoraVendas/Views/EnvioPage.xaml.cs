using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class EnvioPage : ContentPage
{
    readonly EnvioViewModel _vm;
    public EnvioPage(EnvioViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.Carregar(); }
}
