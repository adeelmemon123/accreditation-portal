using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class TaQecDiscussionNote
    {
        public int Id { get; set; }

        public int TaQecReviewId { get; set; }
        public TaQecReview TaQecReview { get; set; } = null!;

        public string AuthorUserId { get; set; } = string.Empty;
        public ApplicationUser AuthorUser { get; set; } = null!;

        public string Note { get; set; } = string.Empty;

        // Optional - ties a note to a specific checklist item; null for a general remark.
        public int? ChecklistItemId { get; set; }
        public ChecklistItem? ChecklistItem { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
