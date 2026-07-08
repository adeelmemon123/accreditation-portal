using Microsoft.AspNetCore.Identity;

namespace accreditation_portal.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        // Relevant for Provincial TEVTA (scopes read-only access) and Institute/QAB (province of the applicant).
        public string? Province { get; set; }

        // Captured during self-registration (Institute/Assessor forms); not applicable to internally provisioned accounts.
        public string? Cnic { get; set; }
        public string? MobileNo { get; set; }
        public string? Tehsil { get; set; }
        public string? InstituteName { get; set; }

        // SectorExpert (Assessor) only - used to match assessors to applications during On-Site Assessment assignment.
        public string? Sector { get; set; }

        // TAQEC role only - distinguishes the Chairperson (can lock the final grade) from regular
        // committee members (can view/discuss only). Checked live via RequireTaQecChairperson, not a
        // separate Identity role, so Admin can toggle it without a role reassignment.
        public bool IsChairperson { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
