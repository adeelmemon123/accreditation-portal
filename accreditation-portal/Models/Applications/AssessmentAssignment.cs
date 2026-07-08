using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class AssessmentAssignment
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public string ConvenerId { get; set; } = string.Empty;
        public ApplicationUser Convener { get; set; } = null!;

        public DateTime? WindowStartAt { get; set; }
        public DateTime? WindowEndAt { get; set; }
        public AssessmentAssignmentStatus Status { get; set; } = AssessmentAssignmentStatus.NotStarted;
        public DateTime CreatedAt { get; set; }

        public ICollection<AssessmentTeamMember> TeamMembers { get; set; } = new List<AssessmentTeamMember>();
        public ICollection<AssessmentFinding> Findings { get; set; } = new List<AssessmentFinding>();
    }
}
