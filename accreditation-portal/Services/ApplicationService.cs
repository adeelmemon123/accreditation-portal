using accreditation_portal.Data;
using accreditation_portal.Models.ApplicationViewModels;
using accreditation_portal.Models.Applications;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    public class ApplicationService : IApplicationService
    {
        private static readonly ApplicationDocumentType[] RequiredDocumentTypes =
        {
            ApplicationDocumentType.RegistrationCertificate,
            ApplicationDocumentType.AffiliationCertificate,
            ApplicationDocumentType.FeeChallan
        };

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IApplicationLogService _logService;

        public ApplicationService(ApplicationDbContext context, IFileStorageService fileStorageService, IApplicationLogService logService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logService = logService;
        }

        public Task<Application?> GetActiveDraftAsync(string userId) =>
            _context.Applications.FirstOrDefaultAsync(a => a.ApplicantUserId == userId && a.Status == ApplicationStatus.Draft);

        public Task<Application?> GetByIdAsync(int id) =>
            _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Include(a => a.Documents)
                .Include(a => a.DeskReview)
                .FirstOrDefaultAsync(a => a.Id == id);

        public Task<ApplicationDocument?> GetDocumentAsync(int documentId) =>
            _context.ApplicationDocuments
                .Include(d => d.Application)
                .FirstOrDefaultAsync(d => d.Id == documentId);

        public Task<List<Application>> GetUserApplicationsAsync(string userId) =>
            _context.Applications
                .Where(a => a.ApplicantUserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

        public async Task<List<Application>> GetSubmittedApplicationsAsync(ApplicationType? type, string? province)
        {
            var query = _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Where(a => a.Status == ApplicationStatus.Submitted);

            if (type.HasValue)
            {
                query = query.Where(a => a.ApplicationType == type.Value);
            }

            var applications = await query.OrderByDescending(a => a.SubmittedAt).ToListAsync();

            if (!string.IsNullOrWhiteSpace(province))
            {
                applications = applications
                    .Where(a => string.Equals(
                        a.ApplicationType == ApplicationType.Institute ? a.InstituteProfile?.Province : a.QABProfile?.Province,
                        province,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return applications;
        }

        public async Task<Dictionary<ApplicationStatus, int>> GetStatusCountsAsync() =>
            await _context.Applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

        public async Task<Dictionary<ApplicationType, int>> GetTypeCountsAsync() =>
            await _context.Applications
                .GroupBy(a => a.ApplicationType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Type, g => g.Count);

        public async Task<Application> StartApplicationAsync(string userId, ApplicationType type, string? ipAddress)
        {
            var existingDraft = await GetActiveDraftAsync(userId);
            if (existingDraft is not null)
            {
                throw new ApplicationOperationException("You already have an application in progress.");
            }

            var now = DateTime.UtcNow;
            var application = new Application
            {
                ApplicantUserId = userId,
                ApplicationType = type,
                Status = ApplicationStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.Created, "Application draft started.", ipAddress);

            return application;
        }

        public async Task UpdateInstituteProfileAsync(int applicationId, string userId, InstituteProfileViewModel model, string? ipAddress)
        {
            var application = await GetDraftForEditAsync(applicationId, userId, ApplicationType.Institute);

            if (application.InstituteProfile is null)
            {
                application.InstituteProfile = new InstituteProfile { ApplicationId = applicationId };
                _context.InstituteProfiles.Add(application.InstituteProfile);
            }

            var profile = application.InstituteProfile;
            profile.Province = model.Province;
            profile.District = model.District;
            profile.Address = model.Address;
            profile.ContactPersonName = model.ContactPersonName;
            profile.ContactPhone = model.ContactPhone;
            profile.ContactEmail = model.ContactEmail;
            profile.RegistrationNumber = model.RegistrationNumber;
            profile.AffiliationBody = model.AffiliationBody;
            profile.EstablishedYear = model.EstablishedYear;
            profile.Sector = model.Sector;

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(applicationId, userId, ApplicationLogAction.ProfileUpdated, "Institute profile details saved.", ipAddress);
        }

        public async Task UpdateQabProfileAsync(int applicationId, string userId, QABProfileViewModel model, string? ipAddress)
        {
            var application = await GetDraftForEditAsync(applicationId, userId, ApplicationType.QAB);

            if (application.QABProfile is null)
            {
                application.QABProfile = new QABProfile { ApplicationId = applicationId };
                _context.QABProfiles.Add(application.QABProfile);
            }

            var profile = application.QABProfile;
            profile.OrganizationName = model.OrganizationName;
            profile.Province = model.Province;
            profile.Address = model.Address;
            profile.ContactPersonName = model.ContactPersonName;
            profile.ContactPhone = model.ContactPhone;
            profile.ContactEmail = model.ContactEmail;
            profile.RegistrationNumber = model.RegistrationNumber;
            profile.ScopeOfAwarding = model.ScopeOfAwarding;
            profile.AccreditingBodyReference = model.AccreditingBodyReference;
            profile.Sector = model.Sector;

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(applicationId, userId, ApplicationLogAction.ProfileUpdated, "QAB profile details saved.", ipAddress);
        }

        public async Task UploadDocumentAsync(int applicationId, string userId, ApplicationDocumentType documentType, IFormFile file, string? ipAddress)
        {
            var application = await GetDraftForEditAsync(applicationId, userId, expectedType: null);

            var stored = await _fileStorageService.SaveAsync(file, $"applications/{applicationId}");

            var existing = application.Documents.FirstOrDefault(d => d.DocumentType == documentType);
            var isReplace = existing is not null;

            if (existing is not null)
            {
                _fileStorageService.Delete(existing.FilePath);
                existing.FileName = file.FileName;
                existing.StoredFileName = stored.StoredFileName;
                existing.FilePath = stored.FilePath;
                existing.FileSizeBytes = stored.FileSizeBytes;
                existing.ContentType = stored.ContentType;
                existing.UploadedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ApplicationDocuments.Add(new ApplicationDocument
                {
                    ApplicationId = applicationId,
                    DocumentType = documentType,
                    FileName = file.FileName,
                    StoredFileName = stored.StoredFileName,
                    FilePath = stored.FilePath,
                    FileSizeBytes = stored.FileSizeBytes,
                    ContentType = stored.ContentType,
                    UploadedAt = DateTime.UtcNow
                });
            }

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                applicationId,
                userId,
                isReplace ? ApplicationLogAction.DocumentReplaced : ApplicationLogAction.DocumentUploaded,
                $"{(isReplace ? "Replaced" : "Uploaded")} {DescribeDocumentType(documentType)}.",
                ipAddress);
        }

        public async Task DeleteDocumentAsync(int applicationId, string userId, ApplicationDocumentType documentType, string? ipAddress)
        {
            var application = await GetDraftForEditAsync(applicationId, userId, expectedType: null);

            var document = application.Documents.FirstOrDefault(d => d.DocumentType == documentType);
            if (document is null)
            {
                throw new ApplicationOperationException("That document has not been uploaded yet.");
            }

            _fileStorageService.Delete(document.FilePath);
            _context.ApplicationDocuments.Remove(document);

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(applicationId, userId, ApplicationLogAction.DocumentDeleted, $"Deleted {DescribeDocumentType(documentType)}.", ipAddress);
        }

        public async Task SubmitAsync(int applicationId, string userId, string? ipAddress)
        {
            var application = await GetDraftForEditAsync(applicationId, userId, expectedType: null);

            var hasProfile = application.ApplicationType == ApplicationType.Institute
                ? application.InstituteProfile is not null
                : application.QABProfile is not null;

            if (!hasProfile)
            {
                throw new ApplicationOperationException("Please complete the profile details before submitting.");
            }

            var uploadedTypes = application.Documents.Select(d => d.DocumentType).ToHashSet();
            var missing = RequiredDocumentTypes.Where(t => !uploadedTypes.Contains(t)).ToList();
            if (missing.Count > 0)
            {
                throw new ApplicationOperationException(
                    $"Please upload all required documents before submitting. Missing: {string.Join(", ", missing.Select(DescribeDocumentType))}.");
            }

            application.Status = ApplicationStatus.Submitted;
            application.SubmittedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(applicationId, userId, ApplicationLogAction.Submitted, "Application submitted for review.", ipAddress);
        }

        private async Task<Application> GetDraftForEditAsync(int applicationId, string userId, ApplicationType? expectedType)
        {
            var application = await GetByIdAsync(applicationId);
            if (application is null || application.ApplicantUserId != userId)
            {
                throw new UnauthorizedAccessException("You do not have access to this application.");
            }

            if (expectedType.HasValue && application.ApplicationType != expectedType.Value)
            {
                throw new UnauthorizedAccessException("This application is not of the expected type.");
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                throw new ApplicationOperationException("This application has already been submitted and can no longer be edited.");
            }

            return application;
        }

        private static string DescribeDocumentType(ApplicationDocumentType type) => type switch
        {
            ApplicationDocumentType.RegistrationCertificate => "Registration Certificate",
            ApplicationDocumentType.AffiliationCertificate => "Affiliation Certificate",
            ApplicationDocumentType.FeeChallan => "Fee Challan",
            _ => type.ToString()
        };
    }
}
