using PanificadoraVendas.Views;

namespace PanificadoraVendas;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("pedido", typeof(PedidoPage));
        Routing.RegisterRoute("clienteform", typeof(ClienteFormPage));
    }
}
