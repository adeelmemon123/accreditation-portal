using accreditation_portal.Models.Applications;

namespace accreditation_portal.Models.SelfAssessmentViewModels
{
    public class ChecklistViewModel
    {
        public int ApplicationId { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public ApplicationStatus Status { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<ChecklistSectionViewModel> Sections { get; set; } = new();

        public bool IsReadOnly => Status != ApplicationStatus.SelfAssessmentInProgress;
        public int TotalItems => Sections.Sum(s => s.Items.Count);
        public int CompletedItems => Sections.Sum(s => s.Items.Count(i => i.Score.HasValue));
    }

    public class ChecklistSectionViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<ChecklistItemRowViewModel> Items { get; set; } = new();
    }

    public class ChecklistItemRowViewModel
    {
        public int ChecklistItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEvidenceRequired { get; set; }
        public int MaxScore { get; set; }
        public int? Score { get; set; }
        public string? Comments { get; set; }
        public List<EvidenceFileViewModel> Evidence { get; set; } = new();
    }

    public class EvidenceFileViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class SaveChecklistProgressViewModel
    {
        public List<ChecklistItemScoreInput> Items { get; set; } = new();
    }

    public class ChecklistItemScoreInput
    {
        public int ChecklistItemId { get; set; }
        public int? Score { get; set; }
        public string? Comments { get; set; }
    }
}
