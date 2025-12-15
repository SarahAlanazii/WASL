using Wasl.Infrastructure;
using Wasl.Models;

namespace Wasl.ViewModels.CompanyVMs
{
    public class CompanyContractsViewModel
    {
        public List<Contract> Contracts { get; set; }
        public string StatusFilter { get; set; }

        public int WaitingPaymentCount
        {
            get
            {
                if (Contracts == null) return 0;
                return Contracts.Count(c =>
                    !c.Invoices.Any(i => i.Payments.Any(p => p.PaymentStatus == AppConstants.PAYMENT_SUCCESSFUL)));
            }
        }
    }
}