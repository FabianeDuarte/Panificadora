using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class RoteiroPage : ContentPage
{
    readonly RoteiroViewModel _vm;
    public RoteiroPage(RoteiroViewModel vm) { InitializeComponent(); BindingContext = _vm = vm; }
    protected override void OnAppearing() { base.OnAppearing(); _vm.Carregar(); }
}
