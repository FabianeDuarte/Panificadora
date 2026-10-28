using PanificadoraVendas.ViewModels;

namespace PanificadoraVendas.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm) { InitializeComponent(); BindingContext = vm; }
}
