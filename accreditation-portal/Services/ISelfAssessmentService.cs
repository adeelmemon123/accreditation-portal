using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;

namespace accreditation_portal.Services
{
    public interface ISelfAssessmentService
    {
        // Submitted -> SelfAssessmentInProgress on first entry; no-op otherwise.
        Task MarkStartedAsync(Application application, string userId, string? ipAddress);

        Task<ChecklistViewModel> BuildViewModelAsync(Application application);

        Task SaveProgressAsync(Application application, string userId, List<ChecklistItemScoreInput> items, string? ipAddress);

        Task UploadEvidenceAsync(Application application, string userId, int checklistItemId, IFormFile file, string? ipAddress);

        Task DeleteEvidenceAsync(Application application, string userId, int evidenceId, string? ipAddress);

        Task<SelfAssessmentEvidence?> GetEvidenceForDownloadAsync(int evidenceId);

        Task SubmitAsync(Application application, string userId, string? ipAddress);

        // Admin-facing read-only listing (all templates, not just active ones) - see README follow-up note
        // about the full template CRUD editor this will eventually back.
        Task<List<ChecklistTemplate>> GetAllTemplatesAsync();
    }
}
