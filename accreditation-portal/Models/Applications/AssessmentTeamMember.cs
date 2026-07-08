using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class AssessmentTeamMember
    {
        public int Id { get; set; }

        public int AssessmentAssignmentId { get; set; }
        public AssessmentAssignment AssessmentAssignment { get; set; } = null!;

        public string AssessorUserId { get; set; } = string.Empty;
        public ApplicationUser AssessorUser { get; set; } = null!;

        public DateTime AssignedAt { get; set; }
    }
}
