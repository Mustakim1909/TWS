using TWS.Interfaces;
using TWS.Models;
using TWS.ViewModels;

namespace TWS.Views;

public partial class CustomerDetailPage : ContentPage
{
    private readonly IWoocommerceServices _woocommerceServices;
    private readonly IPdfService _pdfService;
    private readonly string _page;
	public CustomerDetailPage(IWoocommerceServices woocommerceServices,IPdfService pdfService,string page)
	{
		InitializeComponent();
        _woocommerceServices=woocommerceServices;
        _pdfService=pdfService;
        _page=page;
		BindingContext = new CustomersViewModel(_woocommerceServices,_pdfService,_page);
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Refresh data when page appears
        if (BindingContext is CustomersViewModel viewModel)
        {
           await viewModel.LoadCustomers();
        }
    }
  

}