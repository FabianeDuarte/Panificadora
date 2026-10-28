using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class ClientesPage : ContentPage
{
    readonly ClientesViewModel _vm;
    public ClientesPage(ClientesViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.Carregar(); }
}
