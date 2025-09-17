using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWS.Models
{
    public class OrderCustomer
    {
        public Order Order { get; set; }
        public Customer Customer { get; set; }
    }
}
