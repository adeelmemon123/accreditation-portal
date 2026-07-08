using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.Applications;
using accreditation_portal.Models.AssessmentViewModels;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    // Admin assigns teams/opens windows; the assigned Convener (an Admin user) and assigned Assessors
    // (SectorExpert) share the Findings/Submit/Evidence actions - each of those re-checks membership for
    // the *specific* assignment being accessed, since the role check alone doesn't scope to "assigned to
    // this one". TAQEC is included only so the Evidence action can stream a file for the TA-QEC report.
    [Authorize(Roles = $"{Roles.Admin},{Roles.SectorExpert},{Roles.TAQEC}")]
    public class AssessmentController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IAssessmentService _assessmentService;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssessmentController(
            IApplicationService applicationService,
            IAssessmentService assessmentService,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _assessmentService = assessmentService;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        private string CurrentUserId => _userManager.GetUserId(User)!;
        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Queue()
        {
            ViewBag.NeedsAssignment = await _assessmentService.GetAssignmentQueueAsync();
            var active = await _assessmentService.GetActiveAssignmentsAsync();
            return View(active);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Assign(int applicationId)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            if (application is null)
            {
                return NotFound();
            }

            if (application.AssessmentAssignment is not null)
            {
                var assignment = await _assessmentService.GetAssignmentByIdAsync(application.AssessmentAssignment.Id);
                return View("AssignmentStatus", assignment);
            }

            if (application.Status != ApplicationStatus.WorthyForVisit)
            {
                TempData["Error"] = "Only applications marked Worthy for Visit can be assigned an assessment team.";
                return RedirectToAction(nameof(Queue));
            }

            var model = new CreateAssignmentViewModel
            {
                Application = application,
                Conveners = await _assessmentService.GetConvenerCandidatesAsync(),
                Assessors = await _assessmentService.GetAssessorCandidatesAsync(application)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateAssignment(int applicationId, CreateAssignmentInput model)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _assessmentService.CreateAssignmentAsync(application, model.ConvenerId, model.AssessorUserIds, CurrentUserId, ClientIp);
                TempData["Success"] = "Assessment team assigned.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Assign), new { applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> OpenWindow(int applicationId)
        {
            var application = await _applicationService.GetByIdAsync(applicationId);
            if (application?.AssessmentAssignment is null)
            {
                return NotFound();
            }

            var assignment = await _assessmentService.GetAssignmentByIdAsync(application.AssessmentAssignment.Id);
            if (assignment is null)
            {
                return NotFound();
            }

            try
            {
                await _assessmentService.OpenWindowAsync(assignment, application, CurrentUserId, ClientIp);
                TempData["Success"] = "Assessment window opened - closes in 3 days.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Assign), new { applicationId });
        }

        [HttpGet]
        [Authorize(Roles = Roles.SectorExpert)]
        public async Task<IActionResult> MyAssignments()
        {
            var assignments = await _assessmentService.GetMyAssignmentsAsync(CurrentUserId);
            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> Findings(int assignmentId)
        {
            var assignment = await _assessmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            var (isTeamMember, isConvener) = GetAccess(assignment);
            if (!isTeamMember && !isConvener)
            {
                return Forbid();
            }

            ViewBag.IsTeamMember = isTeamMember;
            ViewBag.IsConvener = isConvener;

            try
            {
                var model = await _assessmentService.BuildFindingsViewModelAsync(assignment, assignment.Application);
                return View(model);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(MyAssignments));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFindings(int assignmentId, SaveFindingsViewModel model)
        {
            var assignment = await _assessmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            var (isTeamMember, _) = GetAccess(assignment);
            if (!isTeamMember)
            {
                return Forbid();
            }

            try
            {
                await _assessmentService.SaveFindingsAsync(assignment, assignment.Application, CurrentUserId, model.Items, ClientIp);
                TempData["Success"] = "Findings saved.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Findings), new { assignmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidence(int assignmentId, int evidenceId)
        {
            var assignment = await _assessmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            var (isTeamMember, _) = GetAccess(assignment);
            if (!isTeamMember)
            {
                return Forbid();
            }

            try
            {
                await _assessmentService.DeleteEvidenceAsync(assignment, CurrentUserId, evidenceId, ClientIp);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Findings), new { assignmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int assignmentId)
        {
            var assignment = await _assessmentService.GetAssignmentByIdAsync(assignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            var (isTeamMember, isConvener) = GetAccess(assignment);
            if (!isTeamMember && !isConvener)
            {
                return Forbid();
            }

            try
            {
                await _assessmentService.SubmitFindingsAsync(assignment, assignment.Application, CurrentUserId, ClientIp);
                TempData["Success"] = "Assessment findings submitted successfully.";
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Findings), new { assignmentId });
        }

        [HttpGet]
        public async Task<IActionResult> Evidence(int evidenceId)
        {
            var evidence = await _assessmentService.GetEvidenceForDownloadAsync(evidenceId);
            if (evidence is null)
            {
                return NotFound();
            }

            var assignment = await _assessmentService.GetAssignmentByIdAsync(evidence.AssessmentFinding.AssessmentAssignmentId);
            if (assignment is null)
            {
                return NotFound();
            }

            var (isTeamMember, isConvener) = GetAccess(assignment);
            if (!isTeamMember && !isConvener && !User.IsInRole(Roles.TAQEC))
            {
                return Forbid();
            }

            var stream = _fileStorageService.OpenRead(evidence.FilePath);
            return File(stream, evidence.ContentType, evidence.FileName);
        }

        private (bool IsTeamMember, bool IsConvener) GetAccess(AssessmentAssignment assignment)
        {
            var isTeamMember = assignment.TeamMembers.Any(m => m.AssessorUserId == CurrentUserId);
            var isConvener = assignment.ConvenerId == CurrentUserId && User.IsInRole(Roles.Admin);
            return (isTeamMember, isConvener);
        }
    }
}
