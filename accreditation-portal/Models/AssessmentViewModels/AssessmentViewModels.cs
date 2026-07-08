using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;

namespace accreditation_portal.Models.AssessmentViewModels
{
    public class AssessorCandidateViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Province { get; set; }
        public string? Sector { get; set; }
        public bool IsMatch { get; set; }
    }

    public class CreateAssignmentViewModel
    {
        public Application Application { get; set; } = null!;
        public List<ApplicationUser> Conveners { get; set; } = new();
        public List<AssessorCandidateViewModel> Assessors { get; set; } = new();
    }

    public class CreateAssignmentInput
    {
        public string ConvenerId { get; set; } = string.Empty;
        public List<string> AssessorUserIds { get; set; } = new();
    }

    public class FindingsViewModel
    {
        public int AssignmentId { get; set; }
        public Application Application { get; set; } = null!;
        public string TemplateName { get; set; } = string.Empty;
        public List<FindingsSectionViewModel> Sections { get; set; } = new();

        public AssessmentAssignmentStatus AssignmentStatus { get; set; }
        public DateTime? WindowEndAt { get; set; }
        public bool IsWindowLive { get; set; }
    }

    public class FindingsSectionViewModel
    {
        public string Title { get; set; } = string.Empty;
        public List<FindingsItemViewModel> Items { get; set; } = new();
    }

    public class FindingsItemViewModel
    {
        public int ChecklistItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxScore { get; set; }
        public string? Strengths { get; set; }
        public string? Weaknesses { get; set; }
        public string? Findings { get; set; }
        public int? RecommendedScore { get; set; }
        public string? LastUpdatedByName { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public List<EvidenceFileViewModel> Evidence { get; set; } = new();
    }

    public class FindingItemInput
    {
        public int ChecklistItemId { get; set; }
        public string? Strengths { get; set; }
        public string? Weaknesses { get; set; }
        public string? Findings { get; set; }
        public int? RecommendedScore { get; set; }
        public IFormFile? EvidenceFile { get; set; }
    }

    public class SaveFindingsViewModel
    {
        public List<FindingItemInput> Items { get; set; } = new();
    }
}
