using Wasl.Models;

namespace Wasl.ViewModels.InvoiceVMs
{
    public class InvoiceViewModel
    {
        public Invoice Invoice { get; set; }
        public Contract Contract { get; set; }
        public Payment Payment { get; set; }
    }
}
