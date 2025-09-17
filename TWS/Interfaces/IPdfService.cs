using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWS.Models;

namespace TWS.Interfaces
{
    public interface IPdfService
    {
        Task<string> GeneratePdfFromView(VisualElement visualElement, Order order, string type);
    }
}
