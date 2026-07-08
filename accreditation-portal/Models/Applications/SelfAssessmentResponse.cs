namespace accreditation_portal.Models.Applications
{
    public class SelfAssessmentResponse
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public int ChecklistItemId { get; set; }
        public ChecklistItem ChecklistItem { get; set; } = null!;

        // Null until the applicant picks a score - lets evidence/comments be saved before scoring,
        // and lets final-submit distinguish "not yet scored" from "genuinely scored".
        public int? Score { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<SelfAssessmentEvidence> Evidence { get; set; } = new List<SelfAssessmentEvidence>();
    }
}
