using System.ComponentModel.DataAnnotations;

namespace accreditation_portal.Models.AccountViewModels
{
    public class InstituteRegisterViewModel
    {
        [Required]
        [Display(Name = "Complete Name of Institute")]
        public string InstituteName { get; set; } = string.Empty;

        [Required]
        public string Tehsil { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "CNIC must be exactly 13 digits.")]
        [RegularExpression(@"^\d{13}$", ErrorMessage = "CNIC must contain 13 digits without any dashes.")]
        [Display(Name = "CNIC No.")]
        public string Cnic { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Mobile No.")]
        public string MobileNo { get; set; } = string.Empty;

        [Required]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 20 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password))]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
