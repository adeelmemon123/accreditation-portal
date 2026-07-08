using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;

namespace accreditation_portal.Models.TaQecViewModels
{
    public class TaQecReportViewModel
    {
        public Application Application { get; set; } = null!;
        public string TemplateName { get; set; } = string.Empty;
        public List<TaQecReportSectionViewModel> Sections { get; set; } = new();

        public string ApplicantName { get; set; } = string.Empty;
        public string? Province { get; set; }
        public string? Sector { get; set; }

        public double? AverageSelfScore { get; set; }
        public double? AverageRecommendedScore { get; set; }
        public int FlaggedItemCount { get; set; }
        public int TotalItemCount { get; set; }

        public int TaQecReviewId { get; set; }
        public TaQecGrade Grade { get; set; }
        public string? RationaleRemarks { get; set; }
        public string? LockedByName { get; set; }
        public DateTime? LockedAt { get; set; }
        public bool IsLocked => LockedAt.HasValue;

        public List<TaQecDiscussionNoteViewModel> DiscussionNotes { get; set; } = new();
    }

    public class TaQecReportSectionViewModel
    {
        public string Title { get; set; } = string.Empty;
        public List<TaQecReportItemViewModel> Items { get; set; } = new();
    }

    public class TaQecReportItemViewModel
    {
        public int ChecklistItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; }

        public int? SelfScore { get; set; }
        public string? SelfComments { get; set; }
        public List<EvidenceFileViewModel> SelfEvidence { get; set; } = new();

        public bool DeskReviewFlagged { get; set; }
        public string? DeskReviewComment { get; set; }

        public string? AssessorStrengths { get; set; }
        public string? AssessorWeaknesses { get; set; }
        public string? AssessorFindings { get; set; }
        public int? RecommendedScore { get; set; }
        public List<EvidenceFileViewModel> AssessorEvidence { get; set; } = new();
    }

    public class TaQecDiscussionNoteViewModel
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string? ChecklistItemTitle { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddDiscussionNoteInput
    {
        public string Note { get; set; } = string.Empty;
        public int? ChecklistItemId { get; set; }
    }

    public class LockGradeInput
    {
        public TaQecGrade Grade { get; set; }
        public string RationaleRemarks { get; set; } = string.Empty;
    }
}
