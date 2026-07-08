using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.AdminViewModels;
using accreditation_portal.Models.Applications;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IApplicationService _applicationService;
        private readonly ISelfAssessmentService _selfAssessmentService;
        private readonly IDeskReviewService _deskReviewService;
        private readonly IAssessmentService _assessmentService;
        private readonly ITaQecService _taQecService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IApplicationService applicationService,
            ISelfAssessmentService selfAssessmentService,
            IDeskReviewService deskReviewService,
            IAssessmentService assessmentService,
            ITaQecService taQecService)
        {
            _userManager = userManager;
            _applicationService = applicationService;
            _selfAssessmentService = selfAssessmentService;
            _deskReviewService = deskReviewService;
            _assessmentService = assessmentService;
            _taQecService = taQecService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var statusCounts = await _applicationService.GetStatusCountsAsync();
            var typeCounts = await _applicationService.GetTypeCountsAsync();

            var assessmentActive = await _assessmentService.GetActiveAssignmentsAsync();

            var model = new AdminDashboardViewModel
            {
                TotalApplications = statusCounts.Values.Sum(),
                TotalInstitute = typeCounts.GetValueOrDefault(ApplicationType.Institute),
                TotalQAB = typeCounts.GetValueOrDefault(ApplicationType.QAB),
                StatusCounts = Enum.GetValues<ApplicationStatus>()
                    .Select(s => new StatusCountViewModel { Status = s, Count = statusCounts.GetValueOrDefault(s) })
                    .Where(c => c.Count > 0)
                    .ToList(),
                DeskReviewPendingCount = (await _deskReviewService.GetQueueAsync()).Count,
                AssessmentNeedsTeamCount = (await _assessmentService.GetAssignmentQueueAsync()).Count,
                AssessmentAwaitingAttentionCount = assessmentActive.Count(a => a.Status == AssessmentAssignmentStatus.WindowClosed),
                TaQecPendingCount = (await _taQecService.GetQueueAsync()).Count,
                WorthyForVisitCount = statusCounts.GetValueOrDefault(ApplicationStatus.WorthyForVisit),
                DeficientCount = statusCounts.GetValueOrDefault(ApplicationStatus.Deficient),
                GradedCount = statusCounts.GetValueOrDefault(ApplicationStatus.TaQecGraded)
            };

            return View(model);
        }

        // Read-only listing so Admin can confirm what's configured - full template CRUD is a follow-up
        // (see README); this seeder-backed data just needs to be visible for now.
        public async Task<IActionResult> ChecklistTemplates()
        {
            var templates = await _selfAssessmentService.GetAllTemplatesAsync();
            return View(templates);
        }

        public async Task<IActionResult> Applications(ApplicationType? type, string? province)
        {
            var applications = await _applicationService.GetSubmittedApplicationsAsync(type, province);
            ViewBag.SelectedType = type;
            ViewBag.SelectedProvince = province;
            return View(applications);
        }

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.OrderBy(u => u.Email).ToList();
            var model = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Province = user.Province,
                    Roles = roles
                });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            ViewBag.Roles = Roles.InternallyProvisioned;
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!Roles.InternallyProvisioned.Contains(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = Roles.InternallyProvisioned;
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Province = model.Province,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                ViewBag.Roles = Roles.InternallyProvisioned;
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditRoles(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = Roles.All.Select(r => new RoleSelection
                {
                    RoleName = r,
                    IsSelected = currentRoles.Contains(r)
                }).ToList(),
                IsChairperson = user.IsChairperson
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoles(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null)
            {
                return NotFound();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();

            var rolesToAdd = selectedRoles.Except(currentRoles);
            var rolesToRemove = currentRoles.Except(selectedRoles);

            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Only meaningful while the user still holds TAQEC - clear it if the role was just removed.
            user.IsChairperson = selectedRoles.Contains(Roles.TAQEC) && model.IsChairperson;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Users));
        }
    }
}
