using System.ComponentModel.DataAnnotations;

namespace accreditation_portal.Models.ApplicationViewModels
{
    public class QABProfileViewModel
    {
        [Required]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        public string Province { get; set; } = string.Empty;

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
        [Display(Name = "Scope of Awarding")]
        public string ScopeOfAwarding { get; set; } = string.Empty;

        [Display(Name = "Accrediting Body Reference")]
        public string? AccreditingBodyReference { get; set; }

        [Required]
        [Display(Name = "Sector")]
        public string Sector { get; set; } = string.Empty;
    }
}
