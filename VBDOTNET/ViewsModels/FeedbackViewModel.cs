using System.ComponentModel.DataAnnotations;

namespace Wasl.ViewModels.CompanyVMs
{
    /// <summary>
    /// Feedback View Model
    /// </summary>
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comments are required")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Comments must be between 10 and 500 characters")]
        public string Comments { get; set; }
    }
}
