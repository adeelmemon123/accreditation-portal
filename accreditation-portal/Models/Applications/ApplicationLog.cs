namespace accreditation_portal.Models.Applications
{
    public class ApplicationLog
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ApplicationLogAction Action { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }
}
