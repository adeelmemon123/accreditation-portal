using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.AssessmentViewModels;

namespace accreditation_portal.Services
{
    public interface IAssessmentService
    {
        // Admin
        Task<List<Application>> GetAssignmentQueueAsync();

        Task<List<AssessmentAssignment>> GetActiveAssignmentsAsync();

        Task<List<ApplicationUser>> GetConvenerCandidatesAsync();

        Task<List<AssessorCandidateViewModel>> GetAssessorCandidatesAsync(Application application);

        Task<AssessmentAssignment> CreateAssignmentAsync(Application application, string convenerId, List<string> assessorUserIds, string actingUserId, string? ipAddress);

        Task OpenWindowAsync(AssessmentAssignment assignment, Application application, string actingUserId, string? ipAddress);

        // Assessor + Convener
        Task<List<AssessmentAssignment>> GetMyAssignmentsAsync(string assessorUserId);

        Task<AssessmentAssignment?> GetAssignmentByIdAsync(int id);

        Task<FindingsViewModel> BuildFindingsViewModelAsync(AssessmentAssignment assignment, Application application);

        // Scores/notes AND any newly-selected evidence files save together in one call - same reasoning as
        // SelfAssessmentService.SaveProgressAsync: a separate evidence-upload request would reload the page
        // and discard any findings just typed but not yet saved.
        Task SaveFindingsAsync(AssessmentAssignment assignment, Application application, string userId, List<FindingItemInput> items, string? ipAddress);

        Task DeleteEvidenceAsync(AssessmentAssignment assignment, string userId, int evidenceId, string? ipAddress);

        Task SubmitFindingsAsync(AssessmentAssignment assignment, Application application, string userId, string? ipAddress);

        Task<AssessmentEvidence?> GetEvidenceForDownloadAsync(int evidenceId);
    }
}
