using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TWS.Models
{
    public class Address : INotifyPropertyChanged
    {
        private string _FirstName;
        [JsonProperty("first_name")]
        public string FirstName
        {
            get => _FirstName;
            set { if (_FirstName != value) { _FirstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); } }
        }

        private string _LastName;
        [JsonProperty("last_name")]
        public string LastName
        {
            get => _LastName;
            set { if (_LastName != value) { _LastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); } }
        }

        private string _company;
        [JsonProperty("company")]
        public string Company
        {
            get => _company;
            set { if (_company != value) { _company = value; OnPropertyChanged(); } }
        }

        private string _address1;
        [JsonProperty("address_1")]
        public string Address1
        {
            get => _address1;
            set
            {
                if (_address1 != value)
                {
                    _address1 = value;
                    OnPropertyChanged(nameof(Address1));
                    OnPropertyChanged(nameof(Add)); // <- Add bhi update kar do
                }
            }
        }

        private string _address2;
        [JsonProperty("address_2")]
        public string Address2
        {
            get => _address2;
            set
            {
                if (_address2 != value)
                {
                    _address2 = value;
                    OnPropertyChanged(nameof(Address2));
                    OnPropertyChanged(nameof(Add)); // <- Add bhi update
                }
            }
        }

        private string _city;
        [JsonProperty("city")]
        public string City
        {
            get => _city;
            set { if (_city != value) { _city = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShippingAddress)); } }
        }

        private string _state;
        [JsonProperty("state")]
        public string State
        {
            get => _state;
            set { if (_state != value) { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShippingAddress)); } }
        }

        private string _postcode;
        [JsonProperty("postcode")]
        public string Postcode
        {
            get => _postcode;
            set
            {
                if (_postcode != value)
                {
                    _postcode = value;
                    OnPropertyChanged(nameof(Postcode));
                    OnPropertyChanged(nameof(Add)); // <- Add bhi update
                }
            }
        }

        private string _country = "India";
        [JsonProperty("country")]
        public string Country
        {
            get => _country;
            set { if (_country != value) { _country = value; OnPropertyChanged(); } }
        }

        private string _email;
        [JsonProperty("email")]
        public string Email
        {
            get => _email;
            set { if (_email != value) { _email = value; OnPropertyChanged(); } }
        }

        private string _phone;
        [JsonProperty("phone")]
        public string Phone
        {
            get => _phone;
            set { if (_phone != value) { _phone = value; OnPropertyChanged(); } }
        }

        // Computed properties
        public string FullName
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    FirstName = "";
                    LastName = "";
                }
                else
                {
                    // Extra spaces remove kar do
                    var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 1)
                    {
                        FirstName = parts[0];
                        LastName = "";
                    }
                    else
                    {
                        FirstName = parts[0];
                        // LastName me baki sab
                        LastName = string.Join(" ", parts.Skip(1));
                    }
                }

                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(FirstName));
                OnPropertyChanged(nameof(LastName));
            }
        }

        public string FullAddress => $"{Address1} {Address2}";
        public string ShippingAddress => $"{City} {State}";
        public string Add
        {
            get
            {
                var parts = new[] { Address1, Address2, Postcode }
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => x.Trim());
                return string.Join(" ", parts);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
