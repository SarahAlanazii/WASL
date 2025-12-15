using Wasl.Models;

namespace Wasl.ViewModels.InvoiceVMs
{
    public class PaymentsListViewModel
    {
        public List<Payment> Payments { get; set; }
        public string StatusFilter { get; set; }
    }
}
