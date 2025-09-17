using TWS.Interfaces;
using TWS.Views;

namespace TWS
{
    public partial class App : Application
    {
        private readonly IWoocommerceServices _woocommerceServices;
        private readonly IPdfService _pdfService;
        public App(IWoocommerceServices woocommerceServices, IPdfService pdfService)
        {
            InitializeComponent();
            _woocommerceServices = woocommerceServices;
            _pdfService = pdfService;

            Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping("ShareInit", (handler, view) => { });
            MainPage = new NavigationPage(new HomePage(_woocommerceServices, _pdfService));
            _pdfService = pdfService;
        }
    }
}
