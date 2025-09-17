using Microsoft.Extensions.Options;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TWS.Interfaces;
using TWS.Models;

namespace TWS.Services
{
    public class WoocommerceServices : IWoocommerceServices
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpClient = new HttpClient();
        public ObservableCollection<LineItem> LineItems { get; set; }
    = new ObservableCollection<LineItem>();

        public WoocommerceServices(IOptions<AppSettings> appsettings)
        {
              _appSettings = appsettings.Value;
            var authData = Encoding.ASCII.GetBytes($"{_appSettings.ConsumerKey}:{_appSettings.ConsumerSecret}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authData));

            // Set timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{id}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var orders = JsonConvert.DeserializeObject<Order>(json);
                return orders;
            }
            return null;
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            try
            {
                int page = 1;
                var allOrders = new List<Order>();
                bool hasMore = true;

                while (hasMore)
                {
                    var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders?per_page=100&page={page}";

                    var response = await _httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var orders = JsonConvert.DeserializeObject<List<Order>>(content);

                        if (orders == null || orders.Count == 0)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            allOrders.AddRange(orders);
                            page++;
                        }
                    }
                    else
                    {
                        // Handle non-success status code
                        Debug.WriteLine($"API request failed with status: {response.StatusCode}");
                        hasMore = false;
                    }

                    await Task.Delay(200); // Rate limiting delay
                }

                return allOrders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching orders: {ex.Message}");
                return new List<Order>();
            }
        }


        //public async Task<bool> UpdateOrderAsync(Order order)
        //{
        //    try
        //    {
        //        var orderData = new
        //        {
        //            payment_method = order.PaymentMethod,
        //            total = order.Total.ToString(),
        //            billing = new
        //            {
        //                first_name = order.Shipping.FullName,
        //                address_1 = order.Shipping.Add,
        //                address_2 = order.Shipping.ShippingAddress,
        //                phone = order.Shipping.Phone
        //            },
        //            shipping = new
        //            {
        //                first_name = order.Shipping.FullName,
        //                address_1 = order.Shipping.Add,
        //                address_2 = order.Shipping.ShippingAddress,
        //                phone = order.Shipping.Phone
        //            },
        //            line_items = order.LineItems.Select(i => new
        //            {
        //                id = i.ProductId,
        //                name = i.Name,
        //                quantity = i.Quantity,
        //                total = i.Total.ToString()
        //            }).ToList()
        //        };

        //        var json = JsonConvert.SerializeObject(orderData);
        //        var content = new StringContent(json, Encoding.UTF8, "application/json");

        //        var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{order.Id}";
        //        var response = await _httpClient.PutAsync(url, content);

        //        return response.IsSuccessStatusCode;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
        public async Task<bool> UpdateOrderAsync(Order order, object metadata)
        {
            try
            {
                // Read Advance value (prefer frontend metadata first)
                decimal advanceValue = 0;
                if (metadata is IEnumerable<object> incomingMetadata)
                {
                    var incomingAdvance = incomingMetadata
                        .OfType<dynamic>()
                        .FirstOrDefault(m => (m.key?.ToString() ?? m.Key?.ToString()) == "Advance");

                    if (incomingAdvance != null)
                    {
                        var val = incomingAdvance.value?.ToString() ?? incomingAdvance.Value?.ToString();
                        decimal.TryParse(val, out advanceValue);
                    }
                }

                // Fallback: if not found in frontend, check backend metadata
                if (advanceValue == 0 && order.MetaData != null)
                {
                    var advanceMeta = order.MetaData.FirstOrDefault(m => m.Key == "Advance");
                    if (advanceMeta != null)
                    {
                        decimal.TryParse(advanceMeta.Value?.ToString(), out advanceValue);
                    }
                }

                // Check advance status from backend metadata
                var advanceAppliedMeta = order.MetaData?.FirstOrDefault(m => m.Key == "_advance_applied");
                var isAdvanceApplied = advanceAppliedMeta?.Value?.ToString() == "true";

                var previousAdvanceMeta = order.MetaData?.FirstOrDefault(m => m.Key == "_previous_advance");
                var previousAdvanceValue = Convert.ToDecimal(previousAdvanceMeta?.Value?.ToString() ?? "0");

                var feeLines = new List<object>();
                var updatedMetadata = new List<object>();

                // Frontend metadata as-is
                if (metadata is IEnumerable<object> existingMetadata)
                {
                    updatedMetadata.AddRange(existingMetadata);
                }

                // Backend metadata
                var backendMetadata = new List<object>();
                if (order.MetaData != null)
                {
                    var existingBackendMeta = order.MetaData
                        .Where(m => m.Key.StartsWith("_"))
                        .Select(m => new { key = m.Key, value = m.Value } as object)
                        .ToList();
                    backendMetadata.AddRange(existingBackendMeta);
                }

                // Non-advance fees
                List<object> nonAdvanceFees = new List<object>();
                if (order.FeeLines != null)
                {
                    nonAdvanceFees = order.FeeLines
                        .Where(f => !string.IsNullOrEmpty(f.Name) && !f.Name.Contains("Advance", StringComparison.OrdinalIgnoreCase))
                        .Select(f => new
                        {
                            id = f.Id,
                            name = f.Name,
                            total = f.Total,
                            tax_status = f.TaxStatus,
                            tax_class = f.TaxClass
                        } as object)
                        .ToList();
                }

                // Add non-advance fees first
                feeLines.AddRange(nonAdvanceFees);

                // Determine netAdvance
                decimal netAdvance;
                if (!isAdvanceApplied)
                {
                    // First-time apply
                    netAdvance = advanceValue;
                }
                else
                {
                    // Already applied, apply only difference
                    netAdvance = advanceValue - previousAdvanceValue;
                }

                // Apply advance fee if netAdvance != 0
                if (netAdvance != 0)
                {
                    feeLines.Add(new
                    {
                        name = "Advance Payment",
                        total = (-netAdvance).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        tax_status = "none",
                        tax_class = ""
                    });
                }

                // Update backend metadata AFTER feeLines
                backendMetadata = backendMetadata
                    .Where(m =>
                    {
                        if (m is Dictionary<string, object> dict)
                        {
                            return !(dict.ContainsKey("key") &&
                                    (dict["key"]?.ToString() == "_advance_applied" ||
                                     dict["key"]?.ToString() == "_previous_advance"));
                        }
                        else if (m.GetType().GetProperty("key") != null)
                        {
                            var keyProp = m.GetType().GetProperty("key");
                            var keyValue = keyProp?.GetValue(m)?.ToString();
                            return !(keyValue == "_advance_applied" || keyValue == "_previous_advance");
                        }
                        return true;
                    })
                    .ToList();

                // Update advance-related backend metadata
                backendMetadata.Add(new { key = "_advance_applied", value = "true" });
                backendMetadata.Add(new { key = "_previous_advance", value = advanceValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) });

                // Optional: Save Advance itself to backend metadata (for persistence)
                backendMetadata.Add(new { key = "Advance", value = advanceValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) });

                // Combine frontend + backend metadata
                var finalMetadata = new List<object>();
                finalMetadata.AddRange(updatedMetadata);
                finalMetadata.AddRange(backendMetadata);

                // Prepare data for WooCommerce API
                var data = new
                {
                    status = order.Status,
                    shipping = new
                    {
                        first_name = order.Shipping?.FirstName,
                        last_name = order.Shipping?.LastName,
                        company = order.Shipping?.Company,
                        address_1 = order.Shipping?.Address1,
                        address_2 = order.Shipping?.Address2,
                        city = order.Shipping?.City,
                        state = order.Shipping?.State,
                        postcode = order.Shipping?.Postcode,
                        country = order.Shipping?.Country,
                        email = order.Shipping?.Email,
                        phone = order.Shipping?.Phone
                    },
                    billing = new
                    {
                        first_name = order.Billing?.FirstName ?? order.Shipping?.FirstName,
                        last_name = order.Billing?.LastName ?? order.Shipping?.LastName,
                        company = order.Billing?.Company ?? order.Shipping?.Company,
                        address_1 = order.Billing?.Address1 ?? order.Shipping?.Address1,
                        address_2 = order.Billing?.Address2 ?? order.Shipping?.Address2,
                        city = order.Billing?.City ?? order.Shipping?.City,
                        state = order.Billing?.State ?? order.Shipping?.State,
                        postcode = order.Billing?.Postcode ?? order.Shipping?.Postcode,
                        country = order.Billing?.Country ?? order.Shipping?.Country,
                        email = order.Billing?.Email ?? order.Shipping?.Email,
                        phone = order.Billing?.Phone ?? order.Shipping?.Phone
                    },
                    line_items = order.LineItems?.Select(li => new
                    {
                        id = li.Id,
                        product_id = li.ProductId,
                        quantity = li.Quantity,
                        price = li.Price,
                        name = li.Name,
                        subtotal = (li.Price * li.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        total = (li.Price * li.Quantity).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    }).ToArray() ?? Array.Empty<object>(),
                    fee_lines = feeLines.ToArray(),
                    shipping_total = order.ShippingTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    discount_total = order.TotalDiscount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    total_tax = order.TotalTax.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    meta_data = finalMetadata
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{order.Id}";
                var response = await _httpClient.PutAsync(url, content);
                string responseBody = await response.Content.ReadAsStringAsync();
                // Debugging
                Debug.WriteLine($"Advance Value: {advanceValue}");
                Debug.WriteLine($"Previous Advance: {previousAdvanceValue}");
                Debug.WriteLine($"Net Advance Applied: {netAdvance}");
                Debug.WriteLine($"Fee Lines Count: {feeLines.Count}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating order: {ex.Message}");
                return false;
            }
        }




        public async Task<bool> RemoveLineItemAsync(int lineId,int orderId)
        {
            try
            {
                var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{orderId}";
                var payload = new
                {
                    line_items = new[]
             {
            new { id = lineId, quantity = 0 }
        }
                };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(url, content);

                string result = await response.Content.ReadAsStringAsync();
                return true;
            }
            catch (Exception ex) { 
                return false;
            }
        }

        public async Task<bool> AddCustomField(int orderId)
        {
            try
            {
                var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{orderId}";
                var payload = new
                {
                    meta_data = new[]
               {
                new { key = "Advance", value = "0" },
                new { key = "_advance_applied", value = "false" },
                new { key = "_previous_advance", value = "0" }
            }
                };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                string result = await response.Content.ReadAsStringAsync();
                return true;
            }catch (Exception ex) { return false; }
        }

        public async Task<bool> AddLineItems(int orderId)
        {
            try
            {
                var url = $"{_appSettings.BaseUrl}wp-json/wc/v3/orders/{orderId}";
                var body = new
                {
                    line_items = LineItems.Select(li => new
                    {
                        product_id = li.ProductId,   
                        quantity = li.Quantity       
                    }).ToList()
                };
                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
