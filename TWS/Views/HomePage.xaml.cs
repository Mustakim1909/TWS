using TWS.Interfaces;

namespace TWS.Views;

public partial class HomePage : ContentPage
{
    private readonly IWoocommerceServices _woocommerceServices;
    private readonly IPdfService _pdfService;
	public HomePage(IWoocommerceServices woocommerceServices,IPdfService pdfService)
	{
		InitializeComponent();
        _woocommerceServices = woocommerceServices; 
        _pdfService = pdfService;
	}
    private async void OnGenerateLabelClicked(object sender, EventArgs e)
    {
        // Navigate to Generate Label page
        await Navigation.PushAsync(new NavigationPage(new CustomerDetailPage(_woocommerceServices,_pdfService,"Label")));
    }

    private async void OnGenerateInvoiceClicked(object sender, EventArgs e)
    {
        // Navigate to Generate Invoice page
        await Navigation.PushAsync(new NavigationPage(new CustomerDetailPage(_woocommerceServices, _pdfService,"Invoice")));
        }
    }