using accreditation_portal.Data;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    public class SelfAssessmentService : ISelfAssessmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly IApplicationLogService _logService;

        public SelfAssessmentService(ApplicationDbContext context, IFileStorageService fileStorageService, IApplicationLogService logService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logService = logService;
        }

        public async Task MarkStartedAsync(Application application, string userId, string? ipAddress)
        {
            if (application.Status != ApplicationStatus.Submitted)
            {
                return;
            }

            application.Status = ApplicationStatus.SelfAssessmentInProgress;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.SelfAssessmentStarted, "Self-assessment checklist opened.", ipAddress);
        }

        public async Task<ChecklistViewModel> BuildViewModelAsync(Application application)
        {
            var template = await GetActiveTemplateAsync(application.ApplicationType);
            if (template is null)
            {
                throw new ApplicationOperationException(
                    "No active self-assessment checklist is configured for this application type yet. Please contact the administrator.");
            }

            var responses = await _context.SelfAssessmentResponses
                .Include(r => r.Evidence)
                .Where(r => r.ApplicationId == application.Id)
                .ToListAsync();

            var responsesByItemId = responses.ToDictionary(r => r.ChecklistItemId);

            var sections = template.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new ChecklistSectionViewModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Items = s.Items
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i =>
                        {
                            responsesByItemId.TryGetValue(i.Id, out var response);
                            return new ChecklistItemRowViewModel
                            {
                                ChecklistItemId = i.Id,
                                Title = i.Title,
                                Description = i.Description,
                                IsEvidenceRequired = i.IsEvidenceRequired,
                                MaxScore = i.MaxScore,
                                Score = response?.Score,
                                Comments = response?.Comments,
                                Evidence = response?.Evidence
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

            return new ChecklistViewModel
            {
                ApplicationId = application.Id,
                ApplicationType = application.ApplicationType,
                Status = application.Status,
                TemplateName = template.Name,
                Sections = sections
            };
        }

        public async Task SaveProgressAsync(Application application, string userId, List<ChecklistItemScoreInput> items, string? ipAddress)
        {
            EnsureInProgress(application);

            var itemIds = items.Select(i => i.ChecklistItemId).ToList();
            var checklistItems = await _context.ChecklistItems
                .Include(i => i.ChecklistSection)
                    .ThenInclude(s => s.ChecklistTemplate)
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            var existingResponses = await _context.SelfAssessmentResponses
                .Where(r => r.ApplicationId == application.Id && itemIds.Contains(r.ChecklistItemId))
                .ToDictionaryAsync(r => r.ChecklistItemId);

            // Validate everything up front so a single bad row (e.g. tampered request) doesn't partially save.
            foreach (var input in items)
            {
                if (!checklistItems.TryGetValue(input.ChecklistItemId, out var checklistItem))
                {
                    throw new ApplicationOperationException("One of the checklist items is no longer valid.");
                }

                var template = checklistItem.ChecklistSection.ChecklistTemplate;
                if (template.ApplicationType != application.ApplicationType || !template.IsActive)
                {
                    throw new ApplicationOperationException("One of the checklist items does not belong to this application's checklist.");
                }

                if (input.Score.HasValue && (input.Score < 0 || input.Score > checklistItem.MaxScore))
                {
                    throw new ApplicationOperationException($"Score for '{checklistItem.Title}' must be between 0 and {checklistItem.MaxScore}.");
                }
            }

            var now = DateTime.UtcNow;
            var scoredCount = 0;
            var uploadedCount = 0;

            foreach (var input in items)
            {
                var comments = string.IsNullOrWhiteSpace(input.Comments) ? null : input.Comments.Trim();
                var hasFile = input.EvidenceFile is { Length: > 0 };
                var hasContent = input.Score.HasValue || comments is not null || hasFile;

                if (!existingResponses.TryGetValue(input.ChecklistItemId, out var response))
                {
                    if (!hasContent)
                    {
                        continue;
                    }

                    response = new SelfAssessmentResponse
                    {
                        ApplicationId = application.Id,
                        ChecklistItemId = input.ChecklistItemId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.SelfAssessmentResponses.Add(response);
                    existingResponses[input.ChecklistItemId] = response;
                }

                response.Score = input.Score;
                response.Comments = comments;
                response.UpdatedAt = now;

                if (input.Score.HasValue)
                {
                    scoredCount++;
                }

                if (hasFile)
                {
                    var stored = await _fileStorageService.SaveAsync(
                        input.EvidenceFile!,
                        $"applications/{application.Id}/self-assessment/{input.ChecklistItemId}");

                    response.Evidence.Add(new SelfAssessmentEvidence
                    {
                        FileName = input.EvidenceFile!.FileName,
                        StoredFileName = stored.StoredFileName,
                        FilePath = stored.FilePath,
                        FileSizeBytes = stored.FileSizeBytes,
                        ContentType = stored.ContentType,
                        UploadedAt = now
                    });
                    uploadedCount++;
                }
            }

            application.UpdatedAt = now;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                application.Id,
                userId,
                ApplicationLogAction.ChecklistItemScored,
                $"Self-assessment progress saved ({scoredCount} of {items.Count} item(s) scored" +
                (uploadedCount > 0 ? $", {uploadedCount} evidence file(s) uploaded" : string.Empty) + ").",
                ipAddress);
        }

        public async Task UploadEvidenceAsync(Application application, string userId, int checklistItemId, IFormFile file, string? ipAddress)
        {
            EnsureInProgress(application);

            var checklistItem = await _context.ChecklistItems
                .Include(i => i.ChecklistSection)
                    .ThenInclude(s => s.ChecklistTemplate)
                .FirstOrDefaultAsync(i => i.Id == checklistItemId);

            if (checklistItem is null)
            {
                throw new ApplicationOperationException("That checklist item could not be found.");
            }

            // Defense-in-depth: the item must belong to the active template for this application's type.
            var template = checklistItem.ChecklistSection.ChecklistTemplate;
            if (template.ApplicationType != application.ApplicationType || !template.IsActive)
            {
                throw new ApplicationOperationException("That checklist item does not belong to this application's checklist.");
            }

            var response = await _context.SelfAssessmentResponses
                .FirstOrDefaultAsync(r => r.ApplicationId == application.Id && r.ChecklistItemId == checklistItemId);

            var now = DateTime.UtcNow;
            if (response is null)
            {
                response = new SelfAssessmentResponse
                {
                    ApplicationId = application.Id,
                    ChecklistItemId = checklistItemId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.SelfAssessmentResponses.Add(response);
                await _context.SaveChangesAsync();
            }

            var stored = await _fileStorageService.SaveAsync(file, $"applications/{application.Id}/self-assessment/{checklistItemId}");

            _context.SelfAssessmentEvidence.Add(new SelfAssessmentEvidence
            {
                SelfAssessmentResponseId = response.Id,
                FileName = file.FileName,
                StoredFileName = stored.StoredFileName,
                FilePath = stored.FilePath,
                FileSizeBytes = stored.FileSizeBytes,
                ContentType = stored.ContentType,
                UploadedAt = now
            });

            application.UpdatedAt = now;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.EvidenceUploaded, $"Evidence uploaded for '{checklistItem.Title}'.", ipAddress);
        }

        public async Task DeleteEvidenceAsync(Application application, string userId, int evidenceId, string? ipAddress)
        {
            EnsureInProgress(application);

            var evidence = await _context.SelfAssessmentEvidence
                .Include(e => e.SelfAssessmentResponse)
                    .ThenInclude(r => r.ChecklistItem)
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.SelfAssessmentResponse.ApplicationId == application.Id);

            if (evidence is null)
            {
                throw new ApplicationOperationException("That evidence file could not be found.");
            }

            _fileStorageService.Delete(evidence.FilePath);
            var itemTitle = evidence.SelfAssessmentResponse.ChecklistItem.Title;
            _context.SelfAssessmentEvidence.Remove(evidence);

            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.EvidenceDeleted, $"Evidence deleted for '{itemTitle}'.", ipAddress);
        }

        public Task<SelfAssessmentEvidence?> GetEvidenceForDownloadAsync(int evidenceId) =>
            _context.SelfAssessmentEvidence
                .Include(e => e.SelfAssessmentResponse)
                    .ThenInclude(r => r.Application)
                .FirstOrDefaultAsync(e => e.Id == evidenceId);

        public async Task SubmitAsync(Application application, string userId, string? ipAddress)
        {
            EnsureInProgress(application);

            var template = await GetActiveTemplateAsync(application.ApplicationType);
            if (template is null)
            {
                throw new ApplicationOperationException(
                    "No active self-assessment checklist is configured for this application type yet. Please contact the administrator.");
            }

            var allItems = template.Sections.SelectMany(s => s.Items).ToList();
            var itemIds = allItems.Select(i => i.Id).ToList();

            var responses = await _context.SelfAssessmentResponses
                .Include(r => r.Evidence)
                .Where(r => r.ApplicationId == application.Id && itemIds.Contains(r.ChecklistItemId))
                .ToDictionaryAsync(r => r.ChecklistItemId);

            var missingScores = new List<string>();
            var missingEvidence = new List<string>();

            foreach (var item in allItems)
            {
                if (!responses.TryGetValue(item.Id, out var response) || !response.Score.HasValue)
                {
                    missingScores.Add(item.Title);
                    continue;
                }

                if (item.IsEvidenceRequired && response.Evidence.Count == 0)
                {
                    missingEvidence.Add(item.Title);
                }
            }

            if (missingScores.Count > 0)
            {
                throw new ApplicationOperationException($"Please score all checklist items before submitting. Missing: {string.Join(", ", missingScores)}.");
            }

            if (missingEvidence.Count > 0)
            {
                throw new ApplicationOperationException($"Please attach evidence for: {string.Join(", ", missingEvidence)}.");
            }

            application.Status = ApplicationStatus.SelfAssessmentSubmitted;
            application.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.SelfAssessmentSubmitted, "Self-assessment submitted.", ipAddress);
        }

        public async Task<List<ChecklistTemplate>> GetAllTemplatesAsync() =>
            await _context.ChecklistTemplates
                .Include(t => t.Sections)
                    .ThenInclude(s => s.Items)
                .OrderBy(t => t.ApplicationType)
                .ThenByDescending(t => t.IsActive)
                .ThenBy(t => t.Name)
                .ToListAsync();

        private async Task<ChecklistTemplate?> GetActiveTemplateAsync(ApplicationType type) =>
            await _context.ChecklistTemplates
                .Include(t => t.Sections)
                    .ThenInclude(s => s.Items)
                .Where(t => t.ApplicationType == type && t.IsActive)
                .FirstOrDefaultAsync();

        private static void EnsureInProgress(Application application)
        {
            if (application.Status != ApplicationStatus.SelfAssessmentInProgress)
            {
                throw new ApplicationOperationException("This self-assessment is read-only and can no longer be edited.");
            }
        }
    }
}
