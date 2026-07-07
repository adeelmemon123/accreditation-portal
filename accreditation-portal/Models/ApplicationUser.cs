using Microsoft.AspNetCore.Identity;

namespace accreditation_portal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Relevant for Provincial TEVTA (scopes read-only access) and Institute/QAB (province of the applicant).
        public string? Province { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
