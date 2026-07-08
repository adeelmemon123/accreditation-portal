using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.DeskReviewViewModels;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    // Desk Review is Admin-only verification of what the applicant already self-reported in Step 2 -
    // no action here ever edits SelfAssessmentResponse/SelfAssessmentEvidence data.
    [Authorize(Roles = Roles.Admin)]
    public class DeskReviewController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IDeskReviewService _deskReviewService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeskReviewController(
            IApplicationService applicationService,
            IDeskReviewService deskReviewService,
            UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _deskReviewService = deskReviewService;
            _userManager = userManager;
        }

        private string CurrentUserId => _userManager.GetUserId(User)!;
        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            var applications = await _deskReviewService.GetQueueAsync();
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Reviewed(DeskReviewDecision? decision, DateTime? from, DateTime? to)
        {
            var applications = await _deskReviewService.GetReviewedAsync(decision, from, to);
            ViewBag.SelectedDecision = decision;
            ViewBag.From = from;
            ViewBag.To = to;
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                var model = await _deskReviewService.OpenForReviewAsync(application, CurrentUserId, ClientIp);
                return View(model);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Queue));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveItemNote(int id, DeskReviewItemNoteInput model)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _deskReviewService.SaveItemNoteAsync(application, CurrentUserId, model.ChecklistItemId, model.Comment, model.IsFlagged, ClientIp);
                TempData["Success"] = "Note saved.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, DeskReviewDecisionInput model)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _deskReviewService.DecideAsync(application, CurrentUserId, model.Decision, model.OverallComments, ClientIp);
                TempData["Success"] = "Decision finalized.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Review), new { id });
        }
    }
}
