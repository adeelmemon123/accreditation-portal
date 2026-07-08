using accreditation_portal.Models.Applications;

namespace accreditation_portal.Models.AdminViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalApplications { get; set; }
        public int TotalInstitute { get; set; }
        public int TotalQAB { get; set; }

        public List<StatusCountViewModel> StatusCounts { get; set; } = new();

        // Actionable queues - what actually needs an Admin (or Admin-adjacent) click right now.
        public int DeskReviewPendingCount { get; set; }
        public int AssessmentNeedsTeamCount { get; set; }
        public int AssessmentAwaitingAttentionCount { get; set; }
        public int TaQecPendingCount { get; set; }

        public int WorthyForVisitCount { get; set; }
        public int DeficientCount { get; set; }
        public int GradedCount { get; set; }

        public int TotalPendingAction =>
            DeskReviewPendingCount + AssessmentNeedsTeamCount + AssessmentAwaitingAttentionCount + TaQecPendingCount;
    }

    public class StatusCountViewModel
    {
        public ApplicationStatus Status { get; set; }
        public int Count { get; set; }
    }
}
