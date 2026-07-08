namespace accreditation_portal.Models.Applications
{
    public class ChecklistTemplate
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public ApplicationType ApplicationType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<ChecklistSection> Sections { get; set; } = new List<ChecklistSection>();
    }
}
