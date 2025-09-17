using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TWS.Models
{
    public class Order : INotifyPropertyChanged
    {
        private int _id;
        [JsonProperty("id")]
        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private int _customerId;
        [JsonProperty("customer_id")]
        public int CustomerId
        {
            get => _customerId;
            set { if (_customerId != value) { _customerId = value; OnPropertyChanged(); } }
        }

        private string _status;
        [JsonProperty("status")]
        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); } }
        }

        private decimal _total;
        [JsonProperty("total")]
        public decimal Total
        {
            get => _total;
            set { if (_total != value) { _total = value; OnPropertyChanged(); } }
        }

        private List<LineItem> _lineItems = new List<LineItem>();
        [JsonProperty("line_items")]
        public List<LineItem> LineItems
        {
            get => _lineItems;
            set { if (_lineItems != value) { _lineItems = value; OnPropertyChanged(); } }
        }
        private List<MetaData> _metadata = new List<MetaData>();
        [JsonProperty("meta_data")]
        public List<MetaData> MetaData
        {
            get => _metadata;
            set { if (_metadata != value) { _metadata = value; OnPropertyChanged(); } }
        }
        private List<FeeLine> _feeLines = new List<FeeLine>();
        [JsonProperty("fee_lines")]
        public List<FeeLine> FeeLines
        {
            get => _feeLines;
            set { if (_feeLines != value) { _feeLines = value; OnPropertyChanged(); } }
        }
        private Address _shipping = new Address();
        [JsonProperty("shipping")]
        public Address Shipping
        {
            get => _shipping;
            set { if (_shipping != value) { _shipping = value; OnPropertyChanged(); } }
        }

        private Address _billing = new Address();
        [JsonProperty("billing")]
        public Address Billing
        {
            get => _billing;
            set { if (_billing != value) { _billing = value; OnPropertyChanged(); } }
        }

        private DateTime _dateCreated;
        [JsonProperty("date_created")]
        public DateTime DateCreated
        {
            get => _dateCreated;
            set { if (_dateCreated != value) { _dateCreated = value; OnPropertyChanged(); } }
        }

        private string _paymentMethod;
        [JsonProperty("payment_method")]
        public string PaymentMethod
        {
            get => _paymentMethod;
            set { if (_paymentMethod != value) { _paymentMethod = value; OnPropertyChanged(); } }
        }

        private decimal _shippingTotal;
        [JsonProperty("shipping_total")]
        public decimal ShippingTotal
        {
            get => _shippingTotal;
            set { if (_shippingTotal != value) { _shippingTotal = value; OnPropertyChanged(); } }
        }

        private decimal _totalTax;
        [JsonProperty("total_tax")]
        public decimal TotalTax
        {
            get => _totalTax;
            set { if (_totalTax != value) { _totalTax = value; OnPropertyChanged(); } }
        }

        private decimal _totalDiscount;
        [JsonProperty("discount_total")]
        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set { if (_totalDiscount != value) { _totalDiscount = value; OnPropertyChanged(); } }
        }
        public string QrData
        {
            get
            {
                // Build the items section
                var items = string.Join("\n", LineItems.ConvertAll(li => $"  - {li.Name} x{li.Quantity} (₹{li.Total:0})"));

                // Build the full structured string
                var qrText =
        $@"ORDER DETAILS
--------------------
Order ID   : {Id}
Customer   : {Shipping.FullName}
Address    : {Shipping.Add}, {Shipping.ShippingAddress}
Phone      : {Shipping.Phone}

Items:
{items}

Total      : ₹{Total:0}
Payment    : {PaymentMethod}";

                return qrText;
            }
        }



        // Computed property
        public Color StatusColor
        {
            get
            {
                return Status switch
                {
                    "Delivered" => Color.FromArgb("#00A650"),
                    "Shipped" => Color.FromArgb("#007185"),
                    "Processing" => Color.FromArgb("#FF9900"),
                    "Cancelled" => Color.FromArgb("#FF0000"),
                    _ => Color.FromArgb("#666666")
                };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class LineItem : INotifyPropertyChanged
    {
        private int _id;
        [JsonProperty("id")]
        public int Id
        {
            get => _id;
            set { if (_id != value) { _id = value; OnPropertyChanged(); } }
        }

        private int _productId;
        [JsonProperty("product_id")]
        public int ProductId
        {
            get => _productId;
            set { if (_productId != value) { _productId = value; OnPropertyChanged(); } }
        }

        private int _quantity;
        [JsonProperty("quantity")]
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        private decimal _price;
        [JsonProperty("price")]
        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        private string _name;
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        // ✅ Computed properties (no setter, always fresh value)
        [JsonProperty("subtotal")]
        public string Subtotal => (Price * Quantity).ToString("F2");

        [JsonProperty("total")]
        public decimal Total => Price * Quantity;

        private Image _images;
        [JsonProperty("image")]
        public Image Images
        {
            get => _images;
            set { if (_images != value) { _images = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class MetaData : INotifyPropertyChanged
    {
        private int _id;
        [JsonProperty("id")]
        public int Id { get => _id; set { if (_id != value) { _id = value; OnPropertyChanged(); } } }

        private string _key;
        [JsonProperty("key")]
        public string Key { get => _key; set { if (_key != value) { _key = value; OnPropertyChanged(); } } }

        private JToken _value;  // ✅ can be string, number, object, or array
        [JsonProperty("value")]
        public JToken Value { get => _value; set { if (_value != value) { _value = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public class FeeLine : INotifyPropertyChanged
    {
        private int _id;
        [JsonProperty("id")]
        public int Id { get => _id; set { if (_id != value) { _id = value; OnPropertyChanged(); } } }

        private string _name;
        [JsonProperty("name")]
        public string Name { get => _name; set { if (_name != value) { _name = value; OnPropertyChanged(); } } }

        private string _total;
        [JsonProperty("total")]
        public string Total { get => _total; set { if (_total != value) { _total = value; OnPropertyChanged(); } } }

        private string _totalTax;
        [JsonProperty("total_tax")]
        public string TotalTax { get => _totalTax; set { if (_totalTax != value) { _totalTax = value; OnPropertyChanged(); } } }

        private string _taxStatus;
        [JsonProperty("tax_status")]
        public string TaxStatus { get => _taxStatus; set { if (_taxStatus != value) { _taxStatus = value; OnPropertyChanged(); } } }

        private string _taxClass;
        [JsonProperty("tax_class")]
        public string TaxClass { get => _taxClass; set { if (_taxClass != value) { _taxClass = value; OnPropertyChanged(); } } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }   

}

