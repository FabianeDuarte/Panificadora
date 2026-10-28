using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class PedidoPage : ContentPage
{
    readonly PedidoViewModel _vm;
    public PedidoPage(PedidoViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.Carregar(); }
}
