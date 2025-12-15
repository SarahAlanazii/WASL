using Wasl.Models;

namespace Wasl.ViewModels.InvoiceVMs
{
    public class InvoicesListViewModel
    {
        public List<Invoice> Invoices { get; set; }
        public string StatusFilter { get; set; }
    }
}
