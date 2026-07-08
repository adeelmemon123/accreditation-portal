namespace accreditation_portal.Models.Applications
{
    public class DeskReviewItemComment
    {
        public int Id { get; set; }

        public int DeskReviewId { get; set; }
        public DeskReview DeskReview { get; set; } = null!;

        public int ChecklistItemId { get; set; }
        public ChecklistItem ChecklistItem { get; set; } = null!;

        public string? Comment { get; set; }
        public bool IsFlagged { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
