namespace accreditation_portal.Models.Applications
{
    public class ApplicationDocument
    {
        public int Id { get; set; }

        public int ApplicationId { get; set; }
        public Application Application { get; set; } = null!;

        public ApplicationDocumentType DocumentType { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }
}
