using System.Collections.ObjectModel;
using TWS.Interfaces;
using TWS.Models;
using TWS.ViewModels;

namespace TWS.Views;

public partial class LabelEditPage : ContentPage
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

    public LabelEditPage(Order order, IWoocommerceServices wooCommerceService)
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