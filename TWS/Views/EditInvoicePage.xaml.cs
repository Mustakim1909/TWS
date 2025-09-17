using System.Collections.ObjectModel;
using TWS.Converters;
using TWS.Interfaces;
using TWS.Models;

namespace TWS.Views;

public partial class EditInvoicePage : ContentPage
{
    public class CustomField
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    private readonly IWoocommerceServices _wooCommerceService;

    public Order Order { get; set; }
    public ObservableCollection<CustomField> CustomFields { get; set; }
    public ObservableCollection<LineItem> LineItems { get; set; }

    public EditInvoicePage(Order order, IWoocommerceServices wooCommerceService)
    {
        InitializeComponent();
        _wooCommerceService = wooCommerceService;
        Order = order;

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
        var firstlineitem = LineItems.FirstOrDefault();
        var price  = lineItems.Price;
        var qty = lineItems.Quantity;
        LineItems.Add(new LineItem
        {
       
            ProductId = firstlineitem.ProductId ,
            Name = lineItems.Name,
            Quantity = qty,
            Price = price,
            Images = firstlineitem.Images,
            //Subtotal = (price * qty).ToString("F2"),
            //Total = (price * qty)

        });
         OnPropertyChanged(nameof(VisibleCustomFields));
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

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
