using accreditation_portal.Models.ApplicationViewModels;
using accreditation_portal.Models.Applications;

namespace accreditation_portal.Services
{
    public interface IApplicationService
    {
        Task<Application?> GetActiveDraftAsync(string userId);

        Task<Application?> GetByIdAsync(int id);

        Task<ApplicationDocument?> GetDocumentAsync(int documentId);

        Task<List<Application>> GetUserApplicationsAsync(string userId);

        Task<List<Application>> GetSubmittedApplicationsAsync(ApplicationType? type, string? province);

        Task<Dictionary<ApplicationStatus, int>> GetStatusCountsAsync();

        Task<Dictionary<ApplicationType, int>> GetTypeCountsAsync();

        Task<Application> StartApplicationAsync(string userId, ApplicationType type, string? ipAddress);

        Task UpdateInstituteProfileAsync(int applicationId, string userId, InstituteProfileViewModel model, string? ipAddress);

        Task UpdateQabProfileAsync(int applicationId, string userId, QABProfileViewModel model, string? ipAddress);

        Task UploadDocumentAsync(int applicationId, string userId, ApplicationDocumentType documentType, IFormFile file, string? ipAddress);

        Task DeleteDocumentAsync(int applicationId, string userId, ApplicationDocumentType documentType, string? ipAddress);

        Task SubmitAsync(int applicationId, string userId, string? ipAddress);
    }
}
