using TWS.Views;

namespace TWS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("InvoicePage", typeof(InvoicePage));
            Routing.RegisterRoute("LabelPage", typeof(LabelPage));
        }
    }
}
