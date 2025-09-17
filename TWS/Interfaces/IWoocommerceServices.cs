using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWS.Models;
namespace TWS.Interfaces
{
    public interface IWoocommerceServices
    {
        Task<List<Order>> GetOrdersAsync();
        Task<Order> GetOrderByIdAsync(int id);
        Task<bool> UpdateOrderAsync(Order order, object metadata);
        Task<bool> RemoveLineItemAsync(int lineId, int orderId);
        Task<bool> AddCustomField(int orderId);
        Task<bool> AddLineItems(int orderId);
    }
}
