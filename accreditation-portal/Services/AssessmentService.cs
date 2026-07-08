using accreditation_portal.Authorization;
using accreditation_portal.Data;
using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.AssessmentViewModels;
using accreditation_portal.Models.SelfAssessmentViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IApplicationLogService _logService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssessmentService(
            ApplicationDbContext context,
            IFileStorageService fileStorageService,
            IApplicationLogService logService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logService = logService;
            _userManager = userManager;
        }

        public Task<List<Application>> GetAssignmentQueueAsync() =>
            _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Where(a => a.Status == ApplicationStatus.WorthyForVisit && a.AssessmentAssignment == null)
                .OrderBy(a => a.UpdatedAt)
                .ToListAsync();

        public Task<List<AssessmentAssignment>> GetActiveAssignmentsAsync() =>
            _context.AssessmentAssignments
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicantUser)
                .Include(a => a.Convener)
                .Include(a => a.TeamMembers)
                    .ThenInclude(m => m.AssessorUser)
                .Where(a => a.Status != AssessmentAssignmentStatus.FindingsSubmitted)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();

        public async Task<List<ApplicationUser>> GetConvenerCandidatesAsync() =>
            (await _userManager.GetUsersInRoleAsync(Roles.Admin))
                .OrderBy(u => u.FullName)
                .ToList();

        public async Task<List<AssessorCandidateViewModel>> GetAssessorCandidatesAsync(Application application)
        {
            var applicationProvince = application.ApplicationType == ApplicationType.Institute
                ? application.InstituteProfile?.Province
                : application.QABProfile?.Province;
            var applicationSector = application.ApplicationType == ApplicationType.Institute
                ? application.InstituteProfile?.Sector
                : application.QABProfile?.Sector;

            var assessors = await _userManager.GetUsersInRoleAsync(Roles.SectorExpert);

            return assessors
                .Select(a => new AssessorCandidateViewModel
                {
                    UserId = a.Id,
                    FullName = a.FullName,
                    Province = a.Province,
                    Sector = a.Sector,
                    IsMatch = !string.IsNullOrWhiteSpace(applicationProvince)
                        && !string.IsNullOrWhiteSpace(applicationSector)
                        && string.Equals(a.Province, applicationProvince, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(a.Sector, applicationSector, StringComparison.OrdinalIgnoreCase)
                })
                .OrderByDescending(a => a.IsMatch)
                .ThenBy(a => a.FullName)
                .ToList();
        }

        public async Task<AssessmentAssignment> CreateAssignmentAsync(Application application, string convenerId, List<string> assessorUserIds, string actingUserId, string? ipAddress)
        {
            if (application.Status != ApplicationStatus.WorthyForVisit)
            {
                throw new ApplicationOperationException("Only applications marked Worthy for Visit can be assigned an assessment team.");
            }

            var alreadyAssigned = await _context.AssessmentAssignments.AnyAsync(a => a.ApplicationId == application.Id);
            if (alreadyAssigned)
            {
                throw new ApplicationOperationException("This application already has an assessment assignment.");
            }

            var distinctAssessorIds = (assessorUserIds ?? new List<string>()).Distinct().ToList();
            if (distinctAssessorIds.Count == 0)
            {
                throw new ApplicationOperationException("Please select at least one assessor.");
            }

            var now = DateTime.UtcNow;
            var assignment = new AssessmentAssignment
            {
                ApplicationId = application.Id,
                ConvenerId = convenerId,
                Status = AssessmentAssignmentStatus.NotStarted,
                CreatedAt = now
            };

            foreach (var assessorId in distinctAssessorIds)
            {
                assignment.TeamMembers.Add(new AssessmentTeamMember
                {
                    AssessorUserId = assessorId,
                    AssignedAt = now
                });
            }

            _context.AssessmentAssignments.Add(assignment);

            application.Status = ApplicationStatus.AssessmentAssigned;
            application.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                application.Id,
                actingUserId,
                ApplicationLogAction.AssessmentAssigned,
                $"Assessment team assigned ({assignment.TeamMembers.Count} assessor(s)).",
                ipAddress);

            return assignment;
        }

        public async Task OpenWindowAsync(AssessmentAssignment assignment, Application application, string actingUserId, string? ipAddress)
        {
            if (assignment.Status != AssessmentAssignmentStatus.NotStarted)
            {
                throw new ApplicationOperationException("The assessment window has already been opened for this application.");
            }

            var now = DateTime.UtcNow;
            assignment.WindowStartAt = now;
            assignment.WindowEndAt = now.AddDays(3);
            assignment.Status = AssessmentAssignmentStatus.WindowOpen;

            application.Status = ApplicationStatus.AssessmentInProgress;
            application.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                application.Id,
                actingUserId,
                ApplicationLogAction.AssessmentWindowOpened,
                $"Assessment window opened, closes {assignment.WindowEndAt:u}.",
                ipAddress);
        }

        public Task<List<AssessmentAssignment>> GetMyAssignmentsAsync(string assessorUserId) =>
            _context.AssessmentAssignments
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicantUser)
                .Include(a => a.TeamMembers)
                .Where(a => a.TeamMembers.Any(m => m.AssessorUserId == assessorUserId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

        public Task<AssessmentAssignment?> GetAssignmentByIdAsync(int id) =>
            _context.AssessmentAssignments
                .Include(a => a.Application)
                    .ThenInclude(app => app.ApplicantUser)
                .Include(a => a.Convener)
                .Include(a => a.TeamMembers)
                    .ThenInclude(m => m.AssessorUser)
                .FirstOrDefaultAsync(a => a.Id == id);

        public async Task<FindingsViewModel> BuildFindingsViewModelAsync(AssessmentAssignment assignment, Application application)
        {
            var template = await GetActiveTemplateAsync(application.ApplicationType);
            if (template is null)
            {
                throw new ApplicationOperationException(
                    "No active self-assessment checklist is configured for this application type - findings cannot be recorded without it.");
            }

            var findings = await _context.AssessmentFindings
                .Include(f => f.Evidence)
                .Include(f => f.SubmittedByUser)
                .Where(f => f.AssessmentAssignmentId == assignment.Id)
                .ToListAsync();

            var findingsByItemId = findings.ToDictionary(f => f.ChecklistItemId);

            var sections = template.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new FindingsSectionViewModel
                {
                    Title = s.Title,
                    Items = s.Items
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i =>
                        {
                            findingsByItemId.TryGetValue(i.Id, out var finding);
                            return new FindingsItemViewModel
                            {
                                ChecklistItemId = i.Id,
                                Title = i.Title,
                                Description = i.Description,
                                MaxScore = i.MaxScore,
                                Strengths = finding?.Strengths,
                                Weaknesses = finding?.Weaknesses,
                                Findings = finding?.Findings,
                                RecommendedScore = finding?.RecommendedScore,
                                LastUpdatedByName = finding?.SubmittedByUser?.FullName,
                                LastUpdatedAt = finding?.UpdatedAt,
                                Evidence = finding?.Evidence
                                    .Select(e => new EvidenceFileViewModel
                                    {
                                        Id = e.Id,
                                        FileName = e.FileName,
                                        FileSizeBytes = e.FileSizeBytes,
                                        UploadedAt = e.UploadedAt
                                    })
                                    .ToList() ?? new List<EvidenceFileViewModel>()
                            };
                        })
                        .ToList()
                })
                .ToList();

            var isWindowLive = assignment.Status == AssessmentAssignmentStatus.WindowOpen
                && assignment.WindowEndAt.HasValue
                && DateTime.UtcNow < assignment.WindowEndAt.Value;

            return new FindingsViewModel
            {
                AssignmentId = assignment.Id,
                Application = application,
                TemplateName = template.Name,
                Sections = sections,
                AssignmentStatus = assignment.Status,
                WindowEndAt = assignment.WindowEndAt,
                IsWindowLive = isWindowLive
            };
        }

        public async Task SaveFindingsAsync(AssessmentAssignment assignment, Application application, string userId, List<FindingItemInput> items, string? ipAddress)
        {
            EnsureWindowOpenForEditing(assignment);

            var itemIds = items.Select(i => i.ChecklistItemId).ToList();
            var checklistItems = await _context.ChecklistItems
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            var existingFindings = await _context.AssessmentFindings
                .Where(f => f.AssessmentAssignmentId == assignment.Id && itemIds.Contains(f.ChecklistItemId))
                .ToDictionaryAsync(f => f.ChecklistItemId);

            foreach (var input in items)
            {
                if (!checklistItems.TryGetValue(input.ChecklistItemId, out var checklistItem))
                {
                    throw new ApplicationOperationException("One of the checklist items is no longer valid.");
                }

                if (input.RecommendedScore.HasValue && (input.RecommendedScore < 0 || input.RecommendedScore > checklistItem.MaxScore))
                {
                    throw new ApplicationOperationException($"Recommended score for '{checklistItem.Title}' must be between 0 and {checklistItem.MaxScore}.");
                }
            }

            var now = DateTime.UtcNow;
            var updatedCount = 0;
            var uploadedCount = 0;

            foreach (var input in items)
            {
                var strengths = string.IsNullOrWhiteSpace(input.Strengths) ? null : input.Strengths.Trim();
                var weaknesses = string.IsNullOrWhiteSpace(input.Weaknesses) ? null : input.Weaknesses.Trim();
                var findingsText = string.IsNullOrWhiteSpace(input.Findings) ? null : input.Findings.Trim();
                var hasFile = input.EvidenceFile is { Length: > 0 };
                var hasContent = strengths is not null || weaknesses is not null || findingsText is not null || input.RecommendedScore.HasValue || hasFile;

                if (!existingFindings.TryGetValue(input.ChecklistItemId, out var finding))
                {
                    if (!hasContent)
                    {
                        continue;
                    }

                    finding = new AssessmentFinding
                    {
                        AssessmentAssignmentId = assignment.Id,
                        ChecklistItemId = input.ChecklistItemId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.AssessmentFindings.Add(finding);
                    existingFindings[input.ChecklistItemId] = finding;
                }

                finding.Strengths = strengths;
                finding.Weaknesses = weaknesses;
                finding.Findings = findingsText;
                finding.RecommendedScore = input.RecommendedScore;
                finding.SubmittedByUserId = userId;
                finding.UpdatedAt = now;
                updatedCount++;

                if (hasFile)
                {
                    var stored = await _fileStorageService.SaveAsync(
                        input.EvidenceFile!,
                        $"applications/{application.Id}/assessment/{input.ChecklistItemId}");

                    finding.Evidence.Add(new AssessmentEvidence
                    {
                        FileName = input.EvidenceFile!.FileName,
                        StoredFileName = stored.StoredFileName,
                        FilePath = stored.FilePath,
                        FileSizeBytes = stored.FileSizeBytes,
                        ContentType = stored.ContentType,
                        UploadedAt = now,
                        UploadedByUserId = userId
                    });
                    uploadedCount++;
                }
            }

            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                application.Id,
                userId,
                ApplicationLogAction.FindingRecorded,
                $"Assessment findings saved ({updatedCount} item(s) updated" +
                (uploadedCount > 0 ? $", {uploadedCount} evidence file(s) uploaded" : string.Empty) + ").",
                ipAddress);
        }

        public async Task DeleteEvidenceAsync(AssessmentAssignment assignment, string userId, int evidenceId, string? ipAddress)
        {
            EnsureWindowOpenForEditing(assignment);

            var evidence = await _context.AssessmentEvidence
                .Include(e => e.AssessmentFinding)
                    .ThenInclude(f => f.ChecklistItem)
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.AssessmentFinding.AssessmentAssignmentId == assignment.Id);

            if (evidence is null)
            {
                throw new ApplicationOperationException("That evidence file could not be found.");
            }

            _fileStorageService.Delete(evidence.FilePath);
            var itemTitle = evidence.AssessmentFinding.ChecklistItem.Title;
            _context.AssessmentEvidence.Remove(evidence);

            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                assignment.ApplicationId,
                userId,
                ApplicationLogAction.AssessmentEvidenceDeleted,
                $"Assessment evidence deleted for '{itemTitle}'.",
                ipAddress);
        }

        public async Task SubmitFindingsAsync(AssessmentAssignment assignment, Application application, string userId, string? ipAddress)
        {
            EnsureWindowOpenForEditing(assignment);

            var template = await GetActiveTemplateAsync(application.ApplicationType);
            if (template is null)
            {
                throw new ApplicationOperationException("No active checklist is configured for this application type.");
            }

            var allItems = template.Sections.SelectMany(s => s.Items).ToList();
            var itemIds = allItems.Select(i => i.Id).ToList();

            var findings = await _context.AssessmentFindings
                .Where(f => f.AssessmentAssignmentId == assignment.Id && itemIds.Contains(f.ChecklistItemId))
                .ToDictionaryAsync(f => f.ChecklistItemId);

            var missing = allItems
                .Where(item => !findings.TryGetValue(item.Id, out var finding) || string.IsNullOrWhiteSpace(finding.Findings))
                .Select(item => item.Title)
                .ToList();

            if (missing.Count > 0)
            {
                throw new ApplicationOperationException(
                    $"Please record findings for all checklist items before submitting. Missing: {string.Join(", ", missing)}.");
            }

            var now = DateTime.UtcNow;
            assignment.Status = AssessmentAssignmentStatus.FindingsSubmitted;
            application.Status = ApplicationStatus.AssessmentSubmitted;
            application.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.AssessmentSubmitted, "Assessment findings submitted.", ipAddress);
        }

        public Task<AssessmentEvidence?> GetEvidenceForDownloadAsync(int evidenceId) =>
            _context.AssessmentEvidence
                .Include(e => e.AssessmentFinding)
                    .ThenInclude(f => f.AssessmentAssignment)
                .FirstOrDefaultAsync(e => e.Id == evidenceId);

        private async Task<ChecklistTemplate?> GetActiveTemplateAsync(ApplicationType type) =>
            await _context.ChecklistTemplates
                .Include(t => t.Sections)
                    .ThenInclude(s => s.Items)
                .Where(t => t.ApplicationType == type && t.IsActive)
                .FirstOrDefaultAsync();

        // The real time-box enforcement: DateTime.UtcNow is always compared against WindowEndAt live, on
        // every write, regardless of what Status currently says - Status can lag behind reality until the
        // background monitor's next sweep flips it to WindowClosed, but that lag can never let a write
        // through, since this check re-derives "is it actually still open" from the timestamp itself.
        private static void EnsureWindowOpenForEditing(AssessmentAssignment assignment)
        {
            if (assignment.Status == AssessmentAssignmentStatus.FindingsSubmitted)
            {
                throw new ApplicationOperationException("Findings have already been submitted and are read-only.");
            }

            if (assignment.Status != AssessmentAssignmentStatus.WindowOpen
                || !assignment.WindowEndAt.HasValue
                || DateTime.UtcNow >= assignment.WindowEndAt.Value)
            {
                throw new ApplicationOperationException("The 3-day assessment window is not open (or has closed). Contact the Admin/Convener.");
            }
        }
    }
}
