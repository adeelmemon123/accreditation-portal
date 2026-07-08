using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class TaQecReview
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public TaQecGrade Grade { get; set; } = TaQecGrade.Pending;
        public string? RationaleRemarks { get; set; }

        public string? LockedByUserId { get; set; }
        public ApplicationUser? LockedByUser { get; set; }
        public DateTime? LockedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<TaQecDiscussionNote> DiscussionNotes { get; set; } = new List<TaQecDiscussionNote>();
    }
}
