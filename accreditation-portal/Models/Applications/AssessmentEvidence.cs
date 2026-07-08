using accreditation_portal.Models;

namespace accreditation_portal.Models.Applications
{
    public class AssessmentEvidence
    {
        public int Id { get; set; }

        public int AssessmentFindingId { get; set; }
        public AssessmentFinding AssessmentFinding { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public string UploadedByUserId { get; set; } = string.Empty;
        public ApplicationUser UploadedByUser { get; set; } = null!;
    }
}
