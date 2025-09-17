using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TWS.Models
{
    public class Customer : INotifyPropertyChanged
    {
        private int _customerId;
        [JsonProperty("customer_id")]
        public int CustomerId
        {
            get => _customerId;
            set { if (_customerId != value) { _customerId = value; OnPropertyChanged(); } }
        }

        private string _email;
        [JsonProperty("email")]
        public string Email
        {
            get => _email;
            set { if (_email != value) { _email = value; OnPropertyChanged(); } }
        }

        private string _firstName;
        [JsonProperty("first_name")]
        public string FirstName
        {
            get => _firstName;
            set { if (_firstName != value) { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); OnPropertyChanged(nameof(Initials)); } }
        }

        private string _lastName;
        [JsonProperty("last_name")]
        public string LastName
        {
            get => _lastName;
            set { if (_lastName != value) { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); OnPropertyChanged(nameof(Initials)); } }
        }

        private string _username;
        [JsonProperty("username")]
        public string Username
        {
            get => _username;
            set { if (_username != value) { _username = value; OnPropertyChanged(); } }
        }

        private Address _billing = new Address();
        [JsonProperty("billing")]
        public Address Billing
        {
            get => _billing;
            set { if (_billing != value) { _billing = value; OnPropertyChanged(); } }
        }

        private Address _shipping = new Address();
        [JsonProperty("shipping")]
        public Address Shipping
        {
            get => _shipping;
            set { if (_shipping != value) { _shipping = value; OnPropertyChanged(); } }
        }

        private int _orderCount;
        public int OrderCount
        {
            get => _orderCount;
            set { if (_orderCount != value) { _orderCount = value; OnPropertyChanged(); } }
        }

        // Computed properties
        public string FullName => $"{FirstName} {LastName}";

        public string Initials =>
            (!string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString() : "") +
            (!string.IsNullOrEmpty(LastName) ? LastName[0].ToString() : "");

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
