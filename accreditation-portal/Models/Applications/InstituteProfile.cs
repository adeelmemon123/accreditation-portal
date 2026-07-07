namespace accreditation_portal.Models.Applications
{
    // InstituteName, Cnic, MobileNo, and Tehsil are not stored here - they live on ApplicationUser
    // (captured at signup) and are read via Application.ApplicantUser wherever this profile is shown.
    public class InstituteProfile
    {
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string AffiliationBody { get; set; } = string.Empty;
        public int EstablishedYear { get; set; }
    }
}
