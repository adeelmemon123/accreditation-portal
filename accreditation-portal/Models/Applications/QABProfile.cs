namespace accreditation_portal.Models.Applications
{
    public class QABProfile
    {
        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public string OrganizationName { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactPersonName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public string ScopeOfAwarding { get; set; } = string.Empty;
        public string? AccreditingBodyReference { get; set; }
    }
}
