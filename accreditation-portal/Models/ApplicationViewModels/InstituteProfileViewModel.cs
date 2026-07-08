using System.ComponentModel.DataAnnotations;

namespace accreditation_portal.Models.ApplicationViewModels
{
    public class InstituteProfileViewModel
    {
        [Required]
        public string Province { get; set; } = string.Empty;

        [Required]
        public string District { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Contact Person Name")]
        public string ContactPersonName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Affiliation Body")]
        public string AffiliationBody { get; set; } = string.Empty;

        [Required]
        [Range(1900, 2100)]
        [Display(Name = "Established Year")]
        public int EstablishedYear { get; set; }

        [Required]
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;
    }
}
