using CommunityToolkit.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TWS.Interfaces;
using TWS.Models;
using TWS.Services;
using TWS.ViewModels;
using TWS.Views;
using ZXing.Net.Maui.Controls;

namespace TWS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().UseBarcodeReader().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit();

#if DEBUG
            var a = Assembly.GetExecutingAssembly();
            using var stream = a.GetManifestResourceStream("TWS.appsettings.json");
            var config = new ConfigurationBuilder().AddJsonStream(stream).Build();
            builder.Configuration.AddConfiguration(config);
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<IWoocommerceServices, WoocommerceServices>();
            builder.Services.AddSingleton<IPdfService, PdfService>();
            builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<CustomerDetailPage>();
            builder.Services.AddTransient<CustomersViewModel>();
            builder.Services.AddTransient<LabelPage>();
            builder.Services.AddTransient<LabelViewModel>();
            builder.Services.AddTransient<LabelEditPage>();
            builder.Services.AddTransient<InvoiceViewModel>();
            builder.Services.AddTransient<InvoicePage>();
            builder.Services.AddTransient<EditInvoicePage>();

            return builder.Build();
        }
    }
}
