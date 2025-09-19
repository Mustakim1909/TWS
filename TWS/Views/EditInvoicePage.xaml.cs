using System.Collections.ObjectModel;
using TWS.Converters;
using TWS.Interfaces;
using TWS.Models;
using TWS.Services;

namespace TWS.Views;

public partial class EditInvoicePage : ContentPage
{
    public class CustomField
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    private readonly IWoocommerceServices _wooCommerceService;
    private readonly IPdfService _pdfService;

    public Order Order { get; set; }
    public ObservableCollection<CustomField> CustomFields { get; set; }
    public ObservableCollection<LineItem> LineItems { get; set; }

    public EditInvoicePage(Order order, IWoocommerceServices wooCommerceService)
    {
        InitializeComponent();
        _wooCommerceService = wooCommerceService;
        Order = order;
        _pdfService = new PdfService();
        CustomFields = new ObservableCollection<CustomField>();
        LineItems = new ObservableCollection<LineItem>(order.LineItems);

       // customFieldsCollection.ItemsSource = CustomFields;
        lineItemsCollection.ItemsSource = LineItems;

        BindingContext = this;

        LoadOrderData();
    }

    private void LoadOrderData()
    {
        lblOrderId.Text = Order.Id.ToString();
        lblDateCreated.Text = Order.DateCreated.ToString("g");
        if (LineItems.Count != 0)
        {
            btnUpdate.Text = "Update Order";
        }
        else
        {
            btnUpdate.Text = "View Invoice";
            Order.Status = "processing";
            ContentPage.Title = "Create Invoice";
        }
        pickerStatus.Items.Add("pending");
        pickerStatus.Items.Add("processing");
        pickerStatus.Items.Add("on-hold");
        pickerStatus.Items.Add("completed");
        pickerStatus.Items.Add("cancelled");
        pickerStatus.Items.Add("refunded");
        pickerStatus.Items.Add("failed");

        var statusIndex = pickerStatus.Items.IndexOf(Order.Status);
        pickerStatus.SelectedIndex = statusIndex >= 0 ? statusIndex : 1;
        
        LoadCustomFields();
    }

    private void LoadCustomFields()
    {
        var advanceField = Order.MetaData?.FirstOrDefault(m => m.Key == "Advance");
        var advanceapplied = Order.MetaData?.FirstOrDefault(m => m.Key == "_advance_applied");
        var previousadvance = Order.MetaData?.FirstOrDefault(m => m.Key == "_previous_advance");
        if (advanceField != null)
            CustomFields.Add(new CustomField { Key = "Advance", Value = advanceField.Value.ToString() });
        else
            CustomFields.Add(new CustomField { Key = "Advance", Value = "0" });
        if (advanceapplied != null)
            CustomFields.Add(new CustomField { Key = "_advance_applied", Value = advanceapplied.Value.ToString() });
        else
            CustomFields.Add(new CustomField { Key = "_advance_applied", Value = "false" });
        if (previousadvance != null)
            CustomFields.Add(new CustomField { Key = "_previous_advance", Value = previousadvance.Value.ToString() });
        else
            CustomFields.Add(new CustomField { Key = "_previous_advance", Value = "0" });
        OnPropertyChanged(nameof(VisibleCustomFields));

    }
    public IEnumerable<CustomField> VisibleCustomFields
    {
        get
        {
            return CustomFields.Where(f => f.Key == "Advance");
        }
    }

    private void OnAddCustomFieldClicked(object sender, EventArgs e)
    {
        CustomFields.Add(new CustomField { Key = "new_field", Value = "" });
    }

    private void OnRemoveCustomFieldClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is CustomField field)
        {
            CustomFields.Remove(field);
        }
    }

    private void OnAddLineItemClicked(object sender, EventArgs e)
    {
        LineItem lineItems = new LineItem();
        if (LineItems.Count != 0)
        {
            var firstlineitem = LineItems.FirstOrDefault();
            var price = lineItems.Price;
            var qty = lineItems.Quantity;
            LineItems.Add(new LineItem
            {

                ProductId = firstlineitem.ProductId,
                Name = lineItems.Name,
                Quantity = qty,
                Price = price,
                Images = firstlineitem.Images,
                //Subtotal = (price * qty).ToString("F2"),
                //Total = (price * qty)

            });
            OnPropertyChanged(nameof(VisibleCustomFields));
        }
        else
        {
            var price = lineItems.Price;
            var qty = lineItems.Quantity;
            LineItems.Add(new LineItem
            {

                Name = lineItems.Name,
                Quantity = qty,
                Price = price,
                //Subtotal = (price * qty).ToString("F2"),
                //Total = (price * qty)

            });
        }
    }

    private async void OnRemoveLineItemClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is LineItem item)
        {
            LineItems.Remove(item);
            await _wooCommerceService.RemoveLineItemAsync(item.Id, Order.Id);
        }
    }

    /// <summary>
    /// Recalculate subtotal + shipping + tax only.
    /// Do NOT subtract discount or advance here.
    /// WooCommerce will handle total calculation.
    /// </summary>
    private void RecalculateOrderTotal()
    {
        decimal subtotal = LineItems.Sum(item => item.Price * item.Quantity);
        Order.Total = subtotal + Order.ShippingTotal + Order.TotalTax;

        OnPropertyChanged(nameof(Order));
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        try
        {
            RecalculateOrderTotal();

            // Update status
            Order.Status = pickerStatus.SelectedItem?.ToString() ?? "pending";

            // Update LineItems
            Order.LineItems = new List<LineItem>(LineItems);

            // Update shipping address
            Order.Shipping.FirstName = entryShippingFirstName.Text;
            Order.Shipping.LastName = entryShippingLastName.Text;
            Order.Shipping.Company = entryShippingCompany.Text;
            Order.Shipping.Address1 = entryShippingAddress1.Text;
            Order.Shipping.Address2 = entryShippingAddress2.Text;
            Order.Shipping.City = entryShippingCity.Text;
            Order.Shipping.State = entryShippingState.Text;
            Order.Shipping.Postcode = entryShippingPostcode.Text;
            Order.Shipping.Country = entryShippingCountry.Text;
            Order.Shipping.Email = entryShippingEmail.Text;
            Order.Shipping.Phone = entryShippingPhone.Text;

            Order.Billing.FirstName = Order.Shipping.FirstName;
            Order.Billing.LastName = Order.Billing.LastName;
            Order.Billing.Company = Order.Shipping.Company;
            Order.Billing.Address1 = Order.Shipping.Address1;
            Order.Billing.Address2 = Order.Shipping.Address2;
            Order.Billing.City = Order.Shipping.City;
            Order.Billing.State = Order.Shipping.State;
            Order.Billing.Postcode = Order.Shipping.Postcode;
            Order.Billing.Country = Order.Shipping.Country;
            Order.Billing.Email = Order.Shipping.Email;
            Order.Billing.Phone = Order.Shipping.Phone;

            // Update amounts from UI
            if (decimal.TryParse(entryShippingTotal.Text, out decimal shippingTotal))
                Order.ShippingTotal = shippingTotal;

            if (decimal.TryParse(entryTotalTax.Text, out decimal totalTax))
                Order.TotalTax = totalTax;

            if (decimal.TryParse(entryTotalDiscount.Text, out decimal totalDiscount))
                Order.TotalDiscount = totalDiscount;

            // Prepare custom fields (metadata)
            var metaData = CustomFields.Select(cf => new
            {
                key = cf.Key,
                value = cf.Value
            }).ToList();

            if (LineItems.Any(li => li.ProductId == 0))
            {
                // Get Advance value from CustomFields
                decimal advanceValue = 0;
                var advanceField = CustomFields.FirstOrDefault(cf => cf.Key == "Advance");
                if (advanceField != null && decimal.TryParse(advanceField.Value, out var adv))
                {
                    advanceValue = adv;
                }
                Order.DateCreated = DateTime.Now;
                Order.MetaData = CustomFields.Select(cf => new MetaData
                {
                    Key = cf.Key,
                    Value = cf.Value
                }).ToList();
                Order.Total = Order.Total - advanceValue;

                OnPrintButtonClicked(this, EventArgs.Empty);
                return;
            }
            // Call API to update order
            var success = await _wooCommerceService.UpdateOrderAsync(Order, metaData);

            if (success)
            {
                await DisplayAlert("Success", "Order updated successfully", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to update order", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
    private async void OnPrintButtonClicked(object sender, EventArgs e)
    {
        try
        {
            btnUpdate.IsEnabled = false;
            btnUpdate.Text = "Generating PDF...";

            await Task.Delay(10);

            var order = Order;
            if (order == null)
            {
                await DisplayAlert("Error", "Order data not found", "OK");
                return;
            }
            await Navigation.PushAsync(new InvoicePage(_wooCommerceService,_pdfService,Order,Order.Id));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
