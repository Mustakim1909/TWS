using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TWS.Interfaces;
using TWS.Models;

namespace TWS.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        private readonly IWoocommerceServices _woocommerceService;
        private Order _order;
        private string _htmlContent;

        public InvoiceViewModel(IWoocommerceServices woocommerceService)
        {
            _woocommerceService = woocommerceService;
            GenerateHtmlCommand = new Command(async () => await GenerateHtml());
        }

        public Order Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public string HtmlContent
        {
            get => _htmlContent;
            set => SetProperty(ref _htmlContent, value);
        }

        public Command GenerateHtmlCommand { get; }

        public async Task LoadOrder(Order order,int orderId)
        {
            Order = await _woocommerceService.GetOrderByIdAsync(orderId);
            if (Order == null) {
                Order = order;
            }
            await GenerateHtml();
        }

        private async Task GenerateHtml()
        {
            var advanceMeta = Order.MetaData?.FirstOrDefault(m => m.Key == "Advance");
            var advanceValue = advanceMeta?.Value ?? "0";
            if (Order == null) return;

            // Load HTML template from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TWS.Resources.Templates.Invoice.html";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var htmlTemplate = await reader.ReadToEndAsync();
                var imageResource = "TWS.Resources.Images.logo.jpg"; // namespace + folder + filename
                using var imageStream = assembly.GetManifestResourceStream(imageResource);
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                string statusBadgeHtml = "";
                if (Order.PaymentMethod.ToLower() == "prepaid")
                {
                    statusBadgeHtml = $"<div class=\"status-badge\"><i class=\"fas fa-check-circle\"></i> Paid</div>";
                }
                // Replace <img src="logo.jpg"/> in HTML with Base64
                htmlTemplate = htmlTemplate.Replace("<img src=\"logo.jpg\"/>",
                                                    $"<img src='data:image/jpeg;base64,{base64}'/>");
                // Replace placeholders with actual order data
                HtmlContent = htmlTemplate
                    .Replace("{{INVOICE_NUMBER}}", Order.Id.ToString())
                    .Replace("{{INVOICE_DATE}}", DateTime.Now.ToString("dd/MM/yyyy"))
                    .Replace("{{DUE_DATE}}", Order.DateCreated.AddDays(30).ToString("MMMM dd, yyyy"))
                    .Replace("{{STATUS}}", Order.Status)
                    .Replace("{{STATUS_CLASS}}", Order.Status.ToLower() == "completed" ? "status-paid" : "status-pending")
                    .Replace("{{CLIENT_NAME}}", $"{Order.Shipping.FirstName} {Order.Shipping.LastName}")
                    .Replace("{{CLIENT_ADDRESS}}", $"{Order.Shipping.Address1} {Order.Shipping.Address2}")
                    .Replace("{{CLIENT_CITY}}", $"{Order.Shipping.City}, {Order.Shipping.State} {Order.Shipping.Postcode}")
                    .Replace("{{CLIENT_EMAIL}}", Order.Billing.Email)
                    .Replace("{{CLIENT_PHONE}}", Order.Billing.Phone)
                    .Replace("{{SUBTOTAL}}", Order.LineItems.Sum(li => li.Price * li.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
                    .Replace("{{TAX_AMOUNT}}", Order.TotalTax.ToString())
                    .Replace("{{DISCOUNT_AMOUNT}}", Order.TotalDiscount.ToString())
                    .Replace("{{PAYMENT_METHOD}}", Order.PaymentMethod.ToUpper())
                    .Replace("{{TOTAL_AMOUNT}}", $"₹{Order.Total}")
                    .Replace("{{ADVANCE_PAYMENT}}", $"{advanceValue}")
                    .Replace("{{STATUS_BADGE}}", statusBadgeHtml);

                // Generate line items HTML
                var lineItemsHtml = new StringBuilder();
                foreach (var item in Order.LineItems)
                {
                    lineItemsHtml.AppendLine($@"
                    <tr>
                        <td>{item.Name}</td>
                        <td class='text-right'>{item.Quantity}</td>
                        <td class='text-right'>₹{item.Price}</td>
                        <td class='text-right'>₹{item.Total}</td>
                    </tr>");
                }

                HtmlContent = HtmlContent.Replace("{{LINE_ITEMS}}", lineItemsHtml.ToString());
            }
        }


    }
}
