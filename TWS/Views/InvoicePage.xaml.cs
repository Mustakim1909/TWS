    using CommunityToolkit.Maui.Alerts;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using TWS.Interfaces;
    using TWS.Models;
    using TWS.ViewModels;

    namespace TWS.Views;

    public partial class InvoicePage : ContentPage
    {
        private readonly InvoiceViewModel _viewModel;
        private readonly IWoocommerceServices _woocommerceService;
        private Order _order;
        private readonly IPdfService _pdfService;
        public InvoicePage(IWoocommerceServices woocommerceServices,IPdfService pdfService,Order order, int orderId)
	    {
		    InitializeComponent();
            _woocommerceService = woocommerceServices;  
            _pdfService = pdfService;
            _order = order;
            _viewModel = new InvoiceViewModel(_woocommerceService);
            BindingContext = _viewModel;
            LoadInvoice(orderId);
        }
        private async void LoadInvoice(int orderId)
        {
            await _viewModel.LoadOrder(_order,orderId);

            //var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Invoice.html");

            //if (File.Exists(htmlPath))
            //{
            //    var html = File.ReadAllText(htmlPath);

            //    InvoiceWebView.Source = new HtmlWebViewSource
            //    {
            //        Html = html,
            //        BaseUrl = FileSystem.AppDataDirectory
            //    };
            //}
                var htmlSource = new HtmlWebViewSource
                {
                    Html = _viewModel.HtmlContent,
                     BaseUrl = FileSystem.AppDataDirectory
                };

                InvoiceWebView.Source = htmlSource;
        }
        private async void OnPrintButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Button disable karein during processing
                PrintButton.IsEnabled = false;
                PrintButton.Text = "Generating PDF...";

                // Small delay for UI update
                await Task.Delay(10);

                Debug.WriteLine($"Starting PDF generation for order: {_order?.Id}");

                // Border element find karein
                var web = this.FindByName<WebView>("InvoiceWebView");

                if (web == null)
                {
                    Debug.WriteLine("Error: Border element not found!");
                    await Toast.Make("Error: Label content not found").Show();
                    return;
                }

                // Ensure border has proper dimensions
                if (web.Width <= 0 || web.Height <= 0)
                {
                    web.WidthRequest = 600;
                    web.HeightRequest = 800;
                    await Task.Delay(50); // Layout update ke liye wait karein
                }

                Debug.WriteLine($"Border size: {web.Width}x{web.Height}");

                // Order data check karein
                if (_order == null)
                {
                    Debug.WriteLine("Error: Order data is null!");
                    await Toast.Make("Error: Order data not available").Show();
                    return;
                }

                // PDF generate karein
                var filePath = await _pdfService.GeneratePdfFromView(web, _order, "Invoice");

                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.WriteLine("Error: PDF file path is empty!");
                    await Toast.Make("Failed to generate PDF").Show();
                    return;
                }

                // Check if file actually created
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"Error: PDF file not found at path: {filePath}");
                    await Toast.Make("PDF file was not created").Show();
                    return;
                }

                Debug.WriteLine($"PDF successfully created at: {filePath}");

                // Share options dikhayein
                var action = await DisplayActionSheet("PDF Ready", "Cancel", null, "Share PDF", "Save PDF");

                if (action == "Share PDF")
                {
                    // PDF share karein
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Shipping Label PDF",
                        File = new ShareFile(filePath)
                    });

                    await Toast.Make("PDF shared successfully").Show();
                }
                else if (action == "Save PDF")
                {
                    // PDF save karein (download folder mein)
                    var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads");
                    if (!Directory.Exists(downloadsPath))
                        Directory.CreateDirectory(downloadsPath);

                    var destinationPath = Path.Combine(downloadsPath, $"Shipping_Label_{_order.Id}.pdf");
                    File.Copy(filePath, destinationPath, true);

                    await Toast.Make($"PDF saved to Downloads folder").Show();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PDF Generation Error: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                await Toast.Make($"Error: {ex.Message}").Show();
            }
            finally
            {
                // Reset button state
                PrintButton.IsEnabled = true;
                PrintButton.Text = "PRINT LABEL";
            }
        }

    }