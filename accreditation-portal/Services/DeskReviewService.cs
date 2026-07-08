using accreditation_portal.Data;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.DeskReviewViewModels;
using Microsoft.EntityFrameworkCore;

namespace accreditation_portal.Services
{
    public class DeskReviewService : IDeskReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISelfAssessmentService _selfAssessmentService;
        private readonly IApplicationLogService _logService;

        public DeskReviewService(ApplicationDbContext context, ISelfAssessmentService selfAssessmentService, IApplicationLogService logService)
        {
            _context = context;
            _selfAssessmentService = selfAssessmentService;
            _logService = logService;
        }

        public Task<List<Application>> GetQueueAsync() =>
            _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Include(a => a.DeskReview)
                .Where(a => a.Status == ApplicationStatus.SelfAssessmentSubmitted || a.Status == ApplicationStatus.UnderDeskReview)
                .OrderBy(a => a.UpdatedAt)
                .ToListAsync();

        public async Task<List<Application>> GetReviewedAsync(DeskReviewDecision? decision, DateTime? from, DateTime? to)
        {
            var query = _context.Applications
                .Include(a => a.ApplicantUser)
                .Include(a => a.InstituteProfile)
                .Include(a => a.QABProfile)
                .Include(a => a.DeskReview)
                .Where(a => a.Status == ApplicationStatus.WorthyForVisit || a.Status == ApplicationStatus.Deficient);

            if (decision.HasValue)
            {
                var status = decision.Value == DeskReviewDecision.WorthyForVisit
                    ? ApplicationStatus.WorthyForVisit
                    : ApplicationStatus.Deficient;
                query = query.Where(a => a.Status == status);
            }

            if (from.HasValue)
            {
                query = query.Where(a => a.DeskReview!.DecidedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(a => a.DeskReview!.DecidedAt <= to.Value);
            }

            return await query.OrderByDescending(a => a.DeskReview!.DecidedAt).ToListAsync();
        }

        public async Task<DeskReviewViewModel> OpenForReviewAsync(Application application, string reviewerId, string? ipAddress)
        {
            var validStatuses = new[]
            {
                ApplicationStatus.SelfAssessmentSubmitted,
                ApplicationStatus.UnderDeskReview,
                ApplicationStatus.WorthyForVisit,
                ApplicationStatus.Deficient
            };

            if (!validStatuses.Contains(application.Status))
            {
                throw new ApplicationOperationException("This application is not yet ready for desk review.");
            }

            var deskReview = await _context.DeskReviews
                .Include(r => r.ItemComments)
                .FirstOrDefaultAsync(r => r.ApplicationId == application.Id);

            var now = DateTime.UtcNow;

            if (deskReview is null)
            {
                if (application.Status != ApplicationStatus.SelfAssessmentSubmitted)
                {
                    throw new ApplicationOperationException("This application's desk review record is missing or inconsistent.");
                }

                deskReview = new DeskReview
                {
                    ApplicationId = application.Id,
                    ReviewerId = reviewerId,
                    StartedAt = now
                };
                _context.DeskReviews.Add(deskReview);

                application.Status = ApplicationStatus.UnderDeskReview;
                application.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await _logService.LogAsync(application.Id, reviewerId, ApplicationLogAction.DeskReviewStarted, "Desk review opened.", ipAddress);
            }

            var checklist = await _selfAssessmentService.BuildViewModelAsync(application);
            var notesByItemId = deskReview.ItemComments.ToDictionary(c => c.ChecklistItemId);

            var sections = checklist.Sections.Select(s => new DeskReviewSectionViewModel
            {
                Title = s.Title,
                Items = s.Items.Select(i =>
                {
                    notesByItemId.TryGetValue(i.ChecklistItemId, out var note);
                    return new DeskReviewItemViewModel
                    {
                        ChecklistItemId = i.ChecklistItemId,
                        Title = i.Title,
                        Description = i.Description,
                        IsEvidenceRequired = i.IsEvidenceRequired,
                        MaxScore = i.MaxScore,
                        Score = i.Score,
                        ApplicantComments = i.Comments,
                        Evidence = i.Evidence,
                        ReviewerComment = note?.Comment,
                        IsFlagged = note?.IsFlagged ?? false
                    };
                }).ToList()
            }).ToList();

            var reviewerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == deskReview.ReviewerId);

            return new DeskReviewViewModel
            {
                Application = application,
                Status = application.Status,
                TemplateName = checklist.TemplateName,
                Sections = sections,
                Decision = deskReview.Decision,
                OverallComments = deskReview.OverallComments,
                ReviewerName = reviewerUser?.FullName,
                StartedAt = deskReview.StartedAt,
                DecidedAt = deskReview.DecidedAt
            };
        }

        public async Task SaveItemNoteAsync(Application application, string reviewerId, int checklistItemId, string? comment, bool isFlagged, string? ipAddress)
        {
            var deskReview = await GetActiveDeskReviewAsync(application);

            var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

            var note = await _context.DeskReviewItemComments
                .FirstOrDefaultAsync(c => c.DeskReviewId == deskReview.Id && c.ChecklistItemId == checklistItemId);

            if (note is null && trimmedComment is null && !isFlagged)
            {
                return;
            }

            if (note is null)
            {
                note = new DeskReviewItemComment
                {
                    DeskReviewId = deskReview.Id,
                    ChecklistItemId = checklistItemId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DeskReviewItemComments.Add(note);
            }

            note.Comment = trimmedComment;
            note.IsFlagged = isFlagged;

            await _context.SaveChangesAsync();

            var itemTitle = await _context.ChecklistItems
                .Where(i => i.Id == checklistItemId)
                .Select(i => i.Title)
                .FirstOrDefaultAsync();

            await _logService.LogAsync(
                application.Id,
                reviewerId,
                isFlagged ? ApplicationLogAction.ItemFlagged : ApplicationLogAction.ItemCommented,
                $"Reviewer note saved for '{itemTitle}'.",
                ipAddress);
        }

        public async Task DecideAsync(Application application, string reviewerId, DeskReviewDecision decision, string overallComments, string? ipAddress)
        {
            var deskReview = await GetActiveDeskReviewAsync(application);

            if (decision != DeskReviewDecision.WorthyForVisit && decision != DeskReviewDecision.Deficient)
            {
                throw new ApplicationOperationException("Please choose Worthy for Visit or Deficient.");
            }

            if (string.IsNullOrWhiteSpace(overallComments))
            {
                throw new ApplicationOperationException("Overall comments are required to finalize a decision.");
            }

            var now = DateTime.UtcNow;
            deskReview.Decision = decision;
            deskReview.OverallComments = overallComments.Trim();
            deskReview.DecidedAt = now;

            application.Status = decision == DeskReviewDecision.WorthyForVisit
                ? ApplicationStatus.WorthyForVisit
                : ApplicationStatus.Deficient;
            application.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _logService.LogAsync(
                application.Id,
                reviewerId,
                ApplicationLogAction.DeskReviewDecisionMade,
                $"Desk review decision: {decision}.",
                ipAddress);
        }

        private async Task<DeskReview> GetActiveDeskReviewAsync(Application application)
        {
            var deskReview = await _context.DeskReviews.FirstOrDefaultAsync(r => r.ApplicationId == application.Id);
            if (deskReview is null)
            {
                throw new ApplicationOperationException("This application has not been opened for desk review yet.");
            }

            if (deskReview.DecidedAt.HasValue)
            {
                throw new ApplicationOperationException("This desk review has already been finalized and is read-only.");
            }

            return deskReview;
        }
    }
}
