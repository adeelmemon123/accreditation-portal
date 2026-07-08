using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;

namespace accreditation_portal.Models.DeskReviewViewModels
{
    public class DeskReviewViewModel
    {
        public Application Application { get; set; } = null!;
        public ApplicationStatus Status { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<DeskReviewSectionViewModel> Sections { get; set; } = new();

        public DeskReviewDecision Decision { get; set; }
        public string? OverallComments { get; set; }
        public string? ReviewerName { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? DecidedAt { get; set; }

        public bool IsDecided => DecidedAt.HasValue;
    }

    public class DeskReviewSectionViewModel
    {
        public string Title { get; set; } = string.Empty;
        public List<DeskReviewItemViewModel> Items { get; set; } = new();
    }

    public class DeskReviewItemViewModel
    {
        public int ChecklistItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsEvidenceRequired { get; set; }
        public int MaxScore { get; set; }
        public int? Score { get; set; }
        public string? ApplicantComments { get; set; }
        public List<EvidenceFileViewModel> Evidence { get; set; } = new();

        public string? ReviewerComment { get; set; }
        public bool IsFlagged { get; set; }
    }

    public class DeskReviewItemNoteInput
    {
        public int ChecklistItemId { get; set; }
        public string? Comment { get; set; }
        public bool IsFlagged { get; set; }
    }

    public class DeskReviewDecisionInput
    {
        public DeskReviewDecision Decision { get; set; }
        public string OverallComments { get; set; } = string.Empty;
    }
}
