namespace accreditation_portal.Models.Applications
{
    public class SelfAssessmentEvidence
    {
        public int Id { get; set; }

        public int SelfAssessmentResponseId { get; set; }
        public SelfAssessmentResponse SelfAssessmentResponse { get; set; } = null!;

        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
