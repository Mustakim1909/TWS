using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TWS.Interfaces;
using TWS.Models;
using TWS.Views;

namespace TWS.ViewModels
{
    public class CustomersViewModel : INotifyPropertyChanged
    {
        private readonly IWoocommerceServices _woocommerceServices;
        private readonly IPdfService _pdfService;
        private bool _isLoading;
        private bool _isRefreshing;
        private string _searchText;
        private string _page;
        private ObservableCollection<Order> _customers;
        public ObservableCollection<Order> _allCustomers { get; set; } = new();
        public CustomersViewModel(IWoocommerceServices woocommerceServices, IPdfService pdfService,string page)
        {
            _woocommerceServices = woocommerceServices;
            _pdfService = pdfService;
            _page = page;
            Customers = new ObservableCollection<Order>();
            RefreshCommand = new Command(async () => await LoadCustomers());
            SearchCommand = new Command<string>(FilterCustomers);
            CustomerSelectedCommand = new Command<Customer>(OnCustomerSelected);
            ViewInvoiceCommand = new Command<Order>(async (order) => await ViewInvoice(order));
            EditInvoiceCommand = new Command<Order>(async (order) => await EditInvoice(order));

            // Load initial data
            LoadCustomers();
            _pdfService = pdfService;
        }

        public ObservableCollection<Order> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Real-time filtering as user types
                FilterCustomers(value);
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public Command<Order> ViewInvoiceCommand { get; }
        public Command<Order> EditInvoiceCommand { get; }
        public ICommand CustomerSelectedCommand { get; }

        public async Task LoadCustomers()
        {
            try
            {
                IsLoading = true;

                // Get customers from WooCommerce service
                var customers = await _woocommerceServices.GetOrdersAsync();
                _allCustomers.Clear();
                foreach (var customer in customers) 
                    _allCustomers.Add(customer);
                Customers = new ObservableCollection<Order>(_allCustomers);
            }
            catch (Exception ex)
            {
                // Error handling would be implemented here
                Console.WriteLine($"Error loading customers: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        private void FilterCustomers(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Agar search khali hai to saare customers dikhao
                Customers = new ObservableCollection<Order>(_allCustomers);
            }
            else
            {
                var filtered = _allCustomers
                    .Where(c =>
                        (!string.IsNullOrEmpty(c.Billing?.FirstName) &&
                         (c.Billing.FirstName + " " + c.Billing.LastName)
                         .Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrEmpty(c.Billing?.Phone) &&
                         c.Billing.Phone.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||

                        (!string.IsNullOrEmpty(c.Billing?.Email) &&
                         c.Billing.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    )
                    .ToList();

                Customers = new ObservableCollection<Order>(filtered);
            }
        }


        private async void OnCustomerSelected(Customer customer)
        {
            if (customer != null)
            {
                // Navigate to customer details page
                // This would be handled by a navigation service in a real application
                Console.WriteLine($"Selected customer: {customer.FullName}");

                // Example navigation (would need to be implemented with your navigation service)
                // await Shell.Current.GoToAsync($"customerdetails?customerId={customer.Id}");
            }
        }

        private async Task ViewInvoice(Order order)
        {
            if (order != null)
            {
                if(_page == "Label")
                {
                    await Application.Current.MainPage.Navigation.PushAsync(
                   new LabelPage(_woocommerceServices, order, _pdfService));
                }
                else if(_page == "Invoice")
                {
                    await Application.Current.MainPage.Navigation.PushAsync(
                        new InvoicePage(_woocommerceServices, _pdfService, order, order.Id));
                }
            }
        }

        private async Task EditInvoice(Order order)
        {
            if (order != null)
            {
                if (_page == "Label")
                {
                    await Application.Current.MainPage.Navigation.PushAsync(
                   new LabelEditPage(order,_woocommerceServices));
                }
                else if (_page == "Invoice")
                {
                    await Application.Current.MainPage.Navigation.PushAsync(
                   new EditInvoicePage(order, _woocommerceServices));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
