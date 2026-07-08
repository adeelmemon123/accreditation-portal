using accreditation_portal.Models.Applications;
using accreditation_portal.Models.TaQecViewModels;

namespace accreditation_portal.Services
{
    public interface ITaQecService
    {
        Task<List<Application>> GetQueueAsync();

        Task<List<Application>> GetGradedAsync(TaQecGrade? grade);

        Task<TaQecReview?> GetReviewByApplicationIdAsync(int applicationId);

        // Creates the TaQecReview record and transitions AssessmentSubmitted -> UnderTaQecReview on first
        // entry only; safe to call again for an application already under (or past) review.
        Task<TaQecReportViewModel> OpenForReviewAsync(Application application, string userId, string? ipAddress);

        Task<TaQecReportViewModel> BuildReportAsync(Application application);

        Task AddDiscussionNoteAsync(TaQecReview review, string authorUserId, string note, int? checklistItemId, string? ipAddress);

        Task LockGradeAsync(TaQecReview review, Application application, string userId, TaQecGrade grade, string rationaleRemarks, string? ipAddress);
    }
}
