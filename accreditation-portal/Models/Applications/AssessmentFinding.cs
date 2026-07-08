using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class AssessmentFinding
    {
        public int Id { get; set; }

        public int AssessmentAssignmentId { get; set; }
        public AssessmentAssignment AssessmentAssignment { get; set; } = null!;

        public int ChecklistItemId { get; set; }
        public ChecklistItem ChecklistItem { get; set; } = null!;

        // Overwritten on every edit - "who last touched this item," not an ownership marker. Any assigned
        // team member may edit any item; this is a team effort, not siloed per person (see prompt rules).
        public string SubmittedByUserId { get; set; } = string.Empty;
        public ApplicationUser SubmittedByUser { get; set; } = null!;

        public string? Strengths { get; set; }
        public string? Weaknesses { get; set; }
        public string? Findings { get; set; }
        public int? RecommendedScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<AssessmentEvidence> Evidence { get; set; } = new List<AssessmentEvidence>();
    }
}
