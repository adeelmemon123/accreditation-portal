using accreditation_portal.Data;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;
using accreditation_portal.Models.TaQecViewModels;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    public class TaQecService : ITaQecService
    {
        private readonly ApplicationDbContext _context;
        private readonly IApplicationLogService _logService;

        public TaQecService(ApplicationDbContext context, IApplicationLogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public Task<List<Application>> GetQueueAsync() =>
            _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Where(a => a.Status == ApplicationStatus.AssessmentSubmitted || a.Status == ApplicationStatus.UnderTaQecReview)
                .OrderBy(a => a.UpdatedAt)
                .ToListAsync();

        public async Task<List<Application>> GetGradedAsync(TaQecGrade? grade)
        {
            var query = _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Include(a => a.TaQecReview)
                .Where(a => a.Status == ApplicationStatus.TaQecGraded);

            if (grade.HasValue)
            {
                query = query.Where(a => a.TaQecReview!.Grade == grade.Value);
            }

            return await query.OrderByDescending(a => a.TaQecReview!.LockedAt).ToListAsync();
        }

        public Task<TaQecReview?> GetReviewByApplicationIdAsync(int applicationId) =>
            _context.TaQecReviews.FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        public async Task<TaQecReportViewModel> OpenForReviewAsync(Application application, string userId, string? ipAddress)
        {
            var validStatuses = new[]
            {
                ApplicationStatus.AssessmentSubmitted,
                ApplicationStatus.UnderTaQecReview,
                ApplicationStatus.TaQecGraded
            };

            if (!validStatuses.Contains(application.Status))
            {
                throw new ApplicationOperationException("This application is not yet ready for TA-QEC review.");
            }

            var review = await _context.TaQecReviews.FirstOrDefaultAsync(r => r.ApplicationId == application.Id);

            if (review is null)
            {
                if (application.Status != ApplicationStatus.AssessmentSubmitted)
                {
                    throw new ApplicationOperationException("This application's TA-QEC review record is missing or inconsistent.");
                }

                var now = DateTime.UtcNow;
                review = new TaQecReview
                {
                    ApplicationId = application.Id,
                    CreatedAt = now
                };
                _context.TaQecReviews.Add(review);

                application.Status = ApplicationStatus.UnderTaQecReview;
                application.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await _logService.LogAsync(application.Id, userId, ApplicationLogAction.TaQecReviewStarted, "TA-QEC review opened.", ipAddress);
            }

            return await BuildReportAsync(application);
        }

        public async Task<TaQecReportViewModel> BuildReportAsync(Application application)
        {
            var template = await GetActiveTemplateAsync(application.ApplicationType);
            if (template is null)
            {
                throw new ApplicationOperationException("No active checklist is configured for this application type.");
            }

            var selfResponses = await _context.SelfAssessmentResponses
                .Include(r => r.Evidence)
                .Where(r => r.ApplicationId == application.Id)
                .ToDictionaryAsync(r => r.ChecklistItemId);

            var deskReviewNotes = await _context.DeskReviewItemComments
                .Where(c => c.DeskReview.ApplicationId == application.Id)
                .ToDictionaryAsync(c => c.ChecklistItemId);

            var assignment = await _context.AssessmentAssignments
                .FirstOrDefaultAsync(a => a.ApplicationId == application.Id);

            var findings = assignment is null
                ? new Dictionary<int, AssessmentFinding>()
                : await _context.AssessmentFindings
                    .Include(f => f.Evidence)
                    .Where(f => f.AssessmentAssignmentId == assignment.Id)
                    .ToDictionaryAsync(f => f.ChecklistItemId);

            var sections = template.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new TaQecReportSectionViewModel
                {
                    Title = s.Title,
                    Items = s.Items
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i =>
                        {
                            selfResponses.TryGetValue(i.Id, out var selfResponse);
                            deskReviewNotes.TryGetValue(i.Id, out var deskNote);
                            findings.TryGetValue(i.Id, out var finding);

                            return new TaQecReportItemViewModel
                            {
                                ChecklistItemId = i.Id,
                                Title = i.Title,
                                Description = i.Description,
                                MaxScore = i.MaxScore,
                                SelfScore = selfResponse?.Score,
                                SelfComments = selfResponse?.Comments,
                                SelfEvidence = selfResponse?.Evidence
                                    .Select(ToEvidenceViewModel)
                                    .ToList() ?? new List<EvidenceFileViewModel>(),
                                DeskReviewFlagged = deskNote?.IsFlagged ?? false,
                                DeskReviewComment = deskNote?.Comment,
                                AssessorStrengths = finding?.Strengths,
                                AssessorWeaknesses = finding?.Weaknesses,
                                AssessorFindings = finding?.Findings,
                                RecommendedScore = finding?.RecommendedScore,
                                AssessorEvidence = finding?.Evidence
                                    .Select(ToEvidenceViewModel)
                                    .ToList() ?? new List<EvidenceFileViewModel>()
                            };
                        })
                        .ToList()
                })
                .ToList();

            var allItems = sections.SelectMany(s => s.Items).ToList();
            var selfScores = allItems.Where(i => i.SelfScore.HasValue).Select(i => (double)i.SelfScore!.Value).ToList();
            var recommendedScores = allItems.Where(i => i.RecommendedScore.HasValue).Select(i => (double)i.RecommendedScore!.Value).ToList();

            var profileProvince = application.ApplicationType == ApplicationType.Institute
                ? application.InstituteProfile?.Province
                : application.QABProfile?.Province;
            var profileSector = application.ApplicationType == ApplicationType.Institute
                ? application.InstituteProfile?.Sector
                : application.QABProfile?.Sector;
            var applicantName = application.ApplicationType == ApplicationType.Institute
                ? (application.ApplicantUser.InstituteName ?? application.ApplicantUser.FullName)
                : (application.QABProfile?.OrganizationName ?? application.ApplicantUser.FullName);

            var review = await _context.TaQecReviews
                .Include(r => r.LockedByUser)
                .Include(r => r.DiscussionNotes).ThenInclude(n => n.AuthorUser)
                .Include(r => r.DiscussionNotes).ThenInclude(n => n.ChecklistItem)
                .FirstOrDefaultAsync(r => r.ApplicationId == application.Id);

            return new TaQecReportViewModel
            {
                Application = application,
                TemplateName = template.Name,
                Sections = sections,
                ApplicantName = applicantName,
                Province = profileProvince,
                Sector = profileSector,
                AverageSelfScore = selfScores.Count > 0 ? selfScores.Average() : null,
                AverageRecommendedScore = recommendedScores.Count > 0 ? recommendedScores.Average() : null,
                FlaggedItemCount = allItems.Count(i => i.DeskReviewFlagged),
                TotalItemCount = allItems.Count,
                TaQecReviewId = review?.Id ?? 0,
                Grade = review?.Grade ?? TaQecGrade.Pending,
                RationaleRemarks = review?.RationaleRemarks,
                LockedByName = review?.LockedByUser?.FullName,
                LockedAt = review?.LockedAt,
                DiscussionNotes = review?.DiscussionNotes
                    .OrderBy(n => n.CreatedAt)
                    .Select(n => new TaQecDiscussionNoteViewModel
                    {
                        Id = n.Id,
                        AuthorName = n.AuthorUser.FullName,
                        Note = n.Note,
                        ChecklistItemTitle = n.ChecklistItem?.Title,
                        CreatedAt = n.CreatedAt
                    })
                    .ToList() ?? new List<TaQecDiscussionNoteViewModel>()
            };
        }

        public async Task AddDiscussionNoteAsync(TaQecReview review, string authorUserId, string note, int? checklistItemId, string? ipAddress)
        {
            if (review.LockedAt.HasValue)
            {
                throw new ApplicationOperationException("This TA-QEC review has already been graded and is read-only.");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                throw new ApplicationOperationException("Please enter a note.");
            }

            _context.TaQecDiscussionNotes.Add(new TaQecDiscussionNote
            {
                TaQecReviewId = review.Id,
                AuthorUserId = authorUserId,
                Note = note.Trim(),
                ChecklistItemId = checklistItemId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _logService.LogAsync(review.ApplicationId, authorUserId, ApplicationLogAction.TaQecDiscussionNoteAdded, "TA-QEC discussion note added.", ipAddress);
        }

        public async Task LockGradeAsync(TaQecReview review, Application application, string userId, TaQecGrade grade, string rationaleRemarks, string? ipAddress)
        {
            if (review.LockedAt.HasValue)
            {
                throw new ApplicationOperationException("This TA-QEC review has already been graded.");
            }

            if (grade == TaQecGrade.Pending)
            {
                throw new ApplicationOperationException("Please select a grade.");
            }

            if (string.IsNullOrWhiteSpace(rationaleRemarks))
            {
                throw new ApplicationOperationException("Rationale/remarks are required to lock a grade.");
            }

            var now = DateTime.UtcNow;
            review.Grade = grade;
            review.RationaleRemarks = rationaleRemarks.Trim();
            review.LockedByUserId = userId;
            review.LockedAt = now;

            application.Status = ApplicationStatus.TaQecGraded;
            application.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync(application.Id, userId, ApplicationLogAction.TaQecGradeLocked, $"TA-QEC grade locked: {grade}.", ipAddress);
        }

        private static EvidenceFileViewModel ToEvidenceViewModel(SelfAssessmentEvidence e) => new()
        {
            Id = e.Id,
            FileName = e.FileName,
            FileSizeBytes = e.FileSizeBytes,
            UploadedAt = e.UploadedAt
        };

        private static EvidenceFileViewModel ToEvidenceViewModel(AssessmentEvidence e) => new()
        {
            Id = e.Id,
            FileName = e.FileName,
            FileSizeBytes = e.FileSizeBytes,
            UploadedAt = e.UploadedAt
        };

        private async Task<ChecklistTemplate?> GetActiveTemplateAsync(ApplicationType type) =>
            await _context.ChecklistTemplates
                .Include(t => t.Sections)
                    .ThenInclude(s => s.Items)
                .Where(t => t.ApplicationType == type && t.IsActive)
                .FirstOrDefaultAsync();
    }
}
