namespace accreditation_portal.Models.Applications
{
    public class Application
    {
        public int Id { get; set; }

        public string ApplicantUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicantUser { get; set; } = null!;

        public ApplicationType ApplicationType { get; set; }
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public InstituteProfile? InstituteProfile { get; set; }
        public QABProfile? QABProfile { get; set; }
        public ICollection<ApplicationDocument> Documents { get; set; } = new List<ApplicationDocument>();
        public ICollection<ApplicationLog> Logs { get; set; } = new List<ApplicationLog>();
        public ICollection<SelfAssessmentResponse> SelfAssessmentResponses { get; set; } = new List<SelfAssessmentResponse>();
        public DeskReview? DeskReview { get; set; }
    }
}
