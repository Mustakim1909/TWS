using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TWS.Interfaces;
using TWS.Models;
using ZXing.Net.Maui;
using Microsoft.Maui.Graphics;

namespace TWS.ViewModels
{
    public class LabelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Order _order;
        private readonly IWoocommerceServices _woocommerceServices;
        public ObservableCollection<LineItem> OrderItems { get; set; }
        public ObservableCollection<Order> Orders { get; set; }

        private ImageSource _qrImage;
        public ImageSource QrImage
        {
            get => _qrImage;
            set { _qrImage = value; OnPropertyChanged(); }
        }
       
        public bool IsViewMode { get; set; }
        public bool IsEditMode => !IsViewMode;
        public ICommand UpdateOrderCommand { get; }
        public LabelViewModel(IWoocommerceServices woocommerceServices,Order order)
        {
            _order = order;
            _woocommerceServices = woocommerceServices; 
            Orders = new ObservableCollection<Order>() { order };
            OrderItems = new ObservableCollection<LineItem>(order.LineItems ?? new List<LineItem>());
            // Orders.Add(order);
            UpdateOrderCommand = new Command(async () => await UpdateOrderAsync());
            //GenerateQr(order,customer);
        }

        public string PaymentMethod
        {
            get => _order.PaymentMethod;
            set { _order.PaymentMethod = value; OnPropertyChanged(); }
        }

        public decimal Total
        {
            get => _order.Total;
            set { _order.Total = value; OnPropertyChanged(); }
        }
        private async Task UpdateOrderAsync()
        {
            // LineItems update
            _order.LineItems.Clear();
            foreach (var item in OrderItems)
                _order.LineItems.Add(item);

            // Shipping properties update
            var shipping = Orders.First().Shipping;
            _order.Shipping.FirstName = shipping.FirstName;
            _order.Shipping.LastName = shipping.LastName;
            _order.Shipping.Address1 = shipping.Address1;
            _order.Shipping.Address2 = shipping.Address2;
            _order.Shipping.City = shipping.City;
            _order.Shipping.State = shipping.State;
            _order.Shipping.Postcode = shipping.Postcode;
            _order.Shipping.Phone = shipping.Phone;

            // Payment & Total
            _order.PaymentMethod = PaymentMethod;
            _order.Total = Total;

            //var success = await _woocommerceServices.UpdateOrderAsync(_order);

            //if (success)
            //    await App.Current.MainPage.DisplayAlert("Success", "Order updated successfully!", "OK");
            //else
            //    await App.Current.MainPage.DisplayAlert("Error", "Failed to update order.", "OK");
        }

     
          
        private string GenerateQr(Order order, Customer customer)
        {
            var qrObject = new
            {
                Customer = new
                {
                    customer.FullName,
                    customer.Email,
                    customer.Username,
                    customer.Shipping?.FullAddress,
                    customer.Shipping?.Phone
                },
                Order = new
                {
                    order.Id,
                    Date = order.DateCreated.ToString("dd MMM yyyy"),
                    Total = order.Total,
                    Items = order.LineItems.Select(i => new { i.Name, i.Quantity, i.Total }).ToList()
                }
            };

            string json = JsonConvert.SerializeObject(qrObject);
            return json;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}

