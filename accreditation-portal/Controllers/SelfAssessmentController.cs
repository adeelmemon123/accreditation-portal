using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.SelfAssessmentViewModels;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    // Self-Assessment is the applicant's own self-report - Admin and TAQEC are only included so the
    // Evidence action below can stream a file for Desk Review/TA-QEC report verification;
    // GetOwnedApplicationAsync's strict ownership check still blocks both from every other (mutating)
    // action here, same pattern as ApplicationsController.
    [Authorize(Roles = $"{Roles.Institute},{Roles.QAB},{Roles.Admin},{Roles.TAQEC}")]
    public class SelfAssessmentController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly ISelfAssessmentService _selfAssessmentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IApplicationLogService _logService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SelfAssessmentController(
            IApplicationService applicationService,
            ISelfAssessmentService selfAssessmentService,
            IFileStorageService fileStorageService,
            IApplicationLogService logService,
            UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _selfAssessmentService = selfAssessmentService;
            _fileStorageService = fileStorageService;
            _logService = logService;
            _userManager = userManager;
        }

        private string CurrentUserId => _userManager.GetUserId(User)!;
        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status == ApplicationStatus.Draft)
            {
                TempData["Error"] = "Complete and submit your Step 1 registration before starting the self-assessment.";
                return RedirectToAction("Index", "Applications");
            }

            if (application.Status != ApplicationStatus.Submitted && application.Status != ApplicationStatus.SelfAssessmentInProgress)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            await _selfAssessmentService.MarkStartedAsync(application, CurrentUserId, ClientIp);

            try
            {
                var model = await _selfAssessmentService.BuildViewModelAsync(application);
                return View(model);
            }
            catch (ApplicationOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View((ChecklistViewModel?)null);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProgress(int id, SaveChecklistProgressViewModel model)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _selfAssessmentService.SaveProgressAsync(application, CurrentUserId, model.Items, ClientIp);
                TempData["Success"] = "Progress saved.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadEvidence(int id, int checklistItemId, IFormFile? file)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(Index), new { id });
            }

            try
            {
                await _selfAssessmentService.UploadEvidenceAsync(application, CurrentUserId, checklistItemId, file, ClientIp);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidence(int id, int evidenceId)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _selfAssessmentService.DeleteEvidenceAsync(application, CurrentUserId, evidenceId, ClientIp);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status == ApplicationStatus.Draft || application.Status == ApplicationStatus.Submitted)
            {
                return RedirectToAction(nameof(Index), new { id });
            }

            try
            {
                var model = await _selfAssessmentService.BuildViewModelAsync(application);
                return View(model);
            }
            catch (ApplicationOperationException ex)
            {
                ViewBag.Error = ex.Message;
                return View((ChecklistViewModel?)null);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _selfAssessmentService.SubmitAsync(application, CurrentUserId, ClientIp);
                TempData["Success"] = "Self-assessment submitted successfully.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Evidence(int evidenceId)
        {
            var evidence = await _selfAssessmentService.GetEvidenceForDownloadAsync(evidenceId);
            if (evidence is null)
            {
                return NotFound();
            }

            var isOwner = evidence.SelfAssessmentResponse.Application.ApplicantUserId == CurrentUserId;
            if (!isOwner && !User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.TAQEC))
            {
                return Forbid();
            }

            if (!isOwner)
            {
                await _logService.LogAsync(
                    evidence.SelfAssessmentResponse.ApplicationId,
                    CurrentUserId,
                    ApplicationLogAction.EvidenceViewedByReviewer,
                    $"Reviewer viewed evidence file '{evidence.FileName}'.",
                    ClientIp);
            }

            var stream = _fileStorageService.OpenRead(evidence.FilePath);
            return File(stream, evidence.ContentType, evidence.FileName);
        }

        private async Task<Application?> GetOwnedApplicationAsync(int id)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application is null || application.ApplicantUserId != CurrentUserId)
            {
                return null;
            }

            return application;
        }
    }
}
