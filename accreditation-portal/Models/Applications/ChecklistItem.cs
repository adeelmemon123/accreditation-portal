namespace accreditation_portal.Models.Applications
{
    public class ChecklistItem
    {
        public int Id { get; set; }

        public int ChecklistSectionId { get; set; }
        public ChecklistSection ChecklistSection { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsEvidenceRequired { get; set; }
        public int MaxScore { get; set; } = 5;
    }
}
