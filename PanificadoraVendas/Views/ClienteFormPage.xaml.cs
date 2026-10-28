using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class ClienteFormPage : ContentPage
{
    public ClienteFormPage(ClienteFormViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
