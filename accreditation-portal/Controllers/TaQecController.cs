using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.TaQecViewModels;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    // Any TAQEC committee member can view the report and discuss - only the Chairperson (TAQEC role AND
    // IsChairperson=true, enforced via the RequireTaQecChairperson policy on LockGrade) can finalize a grade.
    [Authorize(Roles = Roles.TAQEC)]
    public class TaQecController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly ITaQecService _taQecService;
        private readonly ITaQecReportPdfGenerator _pdfGenerator;
        private readonly UserManager<ApplicationUser> _userManager;

        public TaQecController(
            IApplicationService applicationService,
            ITaQecService taQecService,
            ITaQecReportPdfGenerator pdfGenerator,
            UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _taQecService = taQecService;
            _pdfGenerator = pdfGenerator;
            _userManager = userManager;
        }

        private string CurrentUserId => _userManager.GetUserId(User)!;
        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            var applications = await _taQecService.GetQueueAsync();
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Graded(TaQecGrade? grade)
        {
            var applications = await _taQecService.GetGradedAsync(grade);
            ViewBag.SelectedGrade = grade;
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Report(int applicationId)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                var model = await _taQecService.OpenForReviewAsync(application, CurrentUserId, ClientIp);
                ViewBag.IsChairperson = (await _userManager.GetUserAsync(User))?.IsChairperson ?? false;
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
        public async Task<IActionResult> AddDiscussionNote(int applicationId, AddDiscussionNoteInput model)
        {
            var review = await _taQecService.GetReviewByApplicationIdAsync(applicationId);
            if (review is null)
            {
                return NotFound();
            }

            try
            {
                await _taQecService.AddDiscussionNoteAsync(review, CurrentUserId, model.Note, model.ChecklistItemId, ClientIp);
                TempData["Success"] = "Note added.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Report), new { applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "RequireTaQecChairperson")]
        public async Task<IActionResult> LockGrade(int applicationId, LockGradeInput model)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            var review = await _taQecService.GetReviewByApplicationIdAsync(applicationId);
            if (application is null || review is null)
            {
                return NotFound();
            }

            try
            {
                await _taQecService.LockGradeAsync(review, application, CurrentUserId, model.Grade, model.RationaleRemarks, ClientIp);
                TempData["Success"] = "Grade locked successfully.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Report), new { applicationId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int applicationId)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                var model = await _taQecService.BuildReportAsync(application);
                var pdfBytes = _pdfGenerator.Generate(model);
                return File(pdfBytes, "application/pdf", $"TAQEC-Report-Application-{applicationId}.pdf");
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Report), new { applicationId });
            }
        }
    }
}
