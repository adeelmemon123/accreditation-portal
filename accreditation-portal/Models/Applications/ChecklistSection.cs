namespace accreditation_portal.Models.Applications
{
    public class ChecklistSection
    {
        public int Id { get; set; }

        public int ChecklistTemplateId { get; set; }
        public ChecklistTemplate ChecklistTemplate { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
    }
}
