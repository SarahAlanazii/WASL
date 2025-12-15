using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.CompanyVMs
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
