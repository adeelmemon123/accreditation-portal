using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class DeskReview
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser Reviewer { get; set; } = null!;

        public DeskReviewDecision Decision { get; set; } = DeskReviewDecision.Pending;
        public string? OverallComments { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? DecidedAt { get; set; }

        public ICollection<DeskReviewItemComment> ItemComments { get; set; } = new List<DeskReviewItemComment>();
    }
}
