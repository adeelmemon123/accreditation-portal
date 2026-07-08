using accreditation_portal.Models.Applications;
using accreditation_portal.Models.DeskReviewViewModels;

namespace accreditation_portal.Services
{
    public interface IDeskReviewService
    {
        Task<List<Application>> GetQueueAsync();

        Task<List<Application>> GetReviewedAsync(DeskReviewDecision? decision, DateTime? from, DateTime? to);

        // Creates the DeskReview record and transitions SelfAssessmentSubmitted -> UnderDeskReview on first
        // entry only; safe to call again for an application already under (or past) review - no-op on the
        // status/record, just (re)builds the read-only view.
        Task<DeskReviewViewModel> OpenForReviewAsync(Application application, string reviewerId, string? ipAddress);

        Task SaveItemNoteAsync(Application application, string reviewerId, int checklistItemId, string? comment, bool isFlagged, string? ipAddress);

        Task DecideAsync(Application application, string reviewerId, DeskReviewDecision decision, string overallComments, string? ipAddress);
    }
}
