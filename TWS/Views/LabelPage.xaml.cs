using CommunityToolkit.Maui.Alerts;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Diagnostics;
using TWS.Interfaces;
using TWS.Models;
using TWS.ViewModels;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace TWS.Views;

public partial class LabelPage : ContentPage
{
    private readonly LabelViewModel _vm;
    private readonly IWoocommerceServices _woocommerceServices;
    private readonly IPdfService _pdfService;
    private Order _order;

    public LabelPage(IWoocommerceServices woocommerceServices, Order order, IPdfService pdfService)
    {
        InitializeComponent();
        _woocommerceServices = woocommerceServices;
        _pdfService = pdfService;
        _order = order;

        // ViewModel initialize karein
        _vm = new LabelViewModel(_woocommerceServices, order);
        BindingContext = _vm;
    }
    //private async void OnPrintButtonClicked(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        // Capture the label content as an image
    //        var border = this.FindByName<Border>("BorderContent"); // You'll need to add x:Name="LabelBorder" to your Border element
    //        if (border == null) return;

    //        // Capture the screenshot of the label content
    //        var screenshot = await border.CaptureAsync();

    //        // Share the screenshot
    //        await Share.RequestAsync(new ShareFileRequest
    //        {
    //            Title = "Shipping Label",
    //            File = new ShareFile(await SaveScreenshotToFile(screenshot))
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Error sharing label: {ex.Message}");
    //        await DisplayAlert("Error", "Unable to share the label.", "OK");
    //    }
    //}
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
            var border = this.FindByName<Border>("BorderContent");

            if (border == null)
            {
                Debug.WriteLine("Error: Border element not found!");
                await Toast.Make("Error: Label content not found").Show();
                return;
            }

            // Ensure border has proper dimensions
            if (border.Width <= 0 || border.Height <= 0)
            {
                border.WidthRequest = 600;
                border.HeightRequest = 800;
                await Task.Delay(50); // Layout update ke liye wait karein
            }

            Debug.WriteLine($"Border size: {border.Width}x{border.Height}");

            // Order data check karein
            if (_order == null)
            {
                Debug.WriteLine("Error: Order data is null!");
                await Toast.Make("Error: Order data not available").Show();
                return;
            }

            // PDF generate karein
            var filePath = await _pdfService.GeneratePdfFromView(border, _order,"Label");

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

    // Screenshot wala code agar aapko need ho toh
    private async Task<string> SaveScreenshotToFile(IScreenshotResult screenshot)
    {
        // Save the screenshot to a temporary file
        using var stream = await screenshot.OpenReadAsync();
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"shipping_label_{DateTime.Now.Ticks}.png");

        using var fileStream = File.OpenWrite(filePath);
        await stream.CopyToAsync(fileStream);

        return filePath;
    }
}