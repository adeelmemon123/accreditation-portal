using accreditation_portal.Authorization;
using accreditation_portal.Models;
using accreditation_portal.Models.ApplicationViewModels;
using accreditation_portal.Models.Applications;
using accreditation_portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace accreditation_portal.Controllers
{
    // Institute/QAB drive the whole flow; Admin is included only so the Document action below can
    // stream a file for cross-checking - every other action here still narrows to the owning user.
    [Authorize(Roles = $"{Roles.Institute},{Roles.QAB},{Roles.Admin}")]
    public class ApplicationsController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationsController(
            IApplicationService applicationService,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager)
        {
            _applicationService = applicationService;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        private string CurrentUserId => _userManager.GetUserId(User)!;
        private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

        public async Task<IActionResult> Index()
        {
            var applications = await _applicationService.GetUserApplicationsAsync(CurrentUserId);
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> Start()
        {
            var existingDraft = await _applicationService.GetActiveDraftAsync(CurrentUserId);
            if (existingDraft is not null)
            {
                return RedirectToAction(nameof(Profile), new { id = existingDraft.Id });
            }

            ViewBag.AvailableTypes = GetAvailableTypes();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(ApplicationType type)
        {
            if (!GetAvailableTypes().Contains(type))
            {
                return Forbid();
            }

            try
            {
                var application = await _applicationService.StartApplicationAsync(CurrentUserId, type, ClientIp);
                return RedirectToAction(nameof(Profile), new { id = application.Id });
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            return application.ApplicationType == ApplicationType.Institute
                ? RedirectToAction(nameof(InstituteProfile), new { id })
                : RedirectToAction(nameof(QabProfile), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> InstituteProfile(int id)
        {
            var application = await GetOwnedApplicationAsync(id, ApplicationType.Institute);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            ViewBag.ApplicationId = id;
            ViewBag.ApplicantUser = application.ApplicantUser;

            var profile = application.InstituteProfile;
            var model = profile is null
                ? new InstituteProfileViewModel()
                : new InstituteProfileViewModel
                {
                    Province = profile.Province,
                    District = profile.District,
                    Address = profile.Address,
                    ContactPersonName = profile.ContactPersonName,
                    ContactPhone = profile.ContactPhone,
                    ContactEmail = profile.ContactEmail,
                    RegistrationNumber = profile.RegistrationNumber,
                    AffiliationBody = profile.AffiliationBody,
                    EstablishedYear = profile.EstablishedYear
                };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InstituteProfile(int id, InstituteProfileViewModel model)
        {
            var application = await GetOwnedApplicationAsync(id, ApplicationType.Institute);
            if (application is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ApplicationId = id;
                ViewBag.ApplicantUser = application.ApplicantUser;
                return View(model);
            }

            try
            {
                await _applicationService.UpdateInstituteProfileAsync(id, CurrentUserId, model, ClientIp);
                return RedirectToAction(nameof(Documents), new { id });
            }
            catch (ApplicationOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.ApplicationId = id;
                ViewBag.ApplicantUser = application.ApplicantUser;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> QabProfile(int id)
        {
            var application = await GetOwnedApplicationAsync(id, ApplicationType.QAB);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            ViewBag.ApplicationId = id;

            var profile = application.QABProfile;
            var model = profile is null
                ? new QABProfileViewModel()
                : new QABProfileViewModel
                {
                    OrganizationName = profile.OrganizationName,
                    Province = profile.Province,
                    Address = profile.Address,
                    ContactPersonName = profile.ContactPersonName,
                    ContactPhone = profile.ContactPhone,
                    ContactEmail = profile.ContactEmail,
                    RegistrationNumber = profile.RegistrationNumber,
                    ScopeOfAwarding = profile.ScopeOfAwarding,
                    AccreditingBodyReference = profile.AccreditingBodyReference
                };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QabProfile(int id, QABProfileViewModel model)
        {
            var application = await GetOwnedApplicationAsync(id, ApplicationType.QAB);
            if (application is null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ApplicationId = id;
                return View(model);
            }

            try
            {
                await _applicationService.UpdateQabProfileAsync(id, CurrentUserId, model, ClientIp);
                return RedirectToAction(nameof(Documents), new { id });
            }
            catch (ApplicationOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.ApplicationId = id;
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Documents(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Draft)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int id, ApplicationDocumentType documentType, IFormFile? file)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (file is null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(Documents), new { id });
            }

            try
            {
                await _applicationService.UploadDocumentAsync(id, CurrentUserId, documentType, file, ClientIp);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Documents), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, ApplicationDocumentType documentType)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            try
            {
                await _applicationService.DeleteDocumentAsync(id, CurrentUserId, documentType, ClientIp);
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Documents), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            // Admin reaches this page read-only from /Admin/Applications - everyone else must own it.
            var application = User.IsInRole(Roles.Admin)
                ? await _applicationService.GetByIdAsync(id)
                : await GetOwnedApplicationAsync(id);

            if (application is null)
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            try
            {
                await _applicationService.SubmitAsync(id, CurrentUserId, ClientIp);
                return RedirectToAction(nameof(Confirmation), new { id });
            }
            catch (ApplicationOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Review), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var application = await GetOwnedApplicationAsync(id);
            if (application is null)
            {
                return NotFound();
            }

            if (application.Status != ApplicationStatus.Submitted)
            {
                return RedirectToAction(nameof(Review), new { id });
            }

            return View(application);
        }

        [HttpGet]
        public async Task<IActionResult> Document(int documentId)
        {
            var document = await _applicationService.GetDocumentAsync(documentId);
            if (document is null)
            {
                return NotFound();
            }

            var isOwner = document.Application.ApplicantUserId == CurrentUserId;
            if (!isOwner && !User.IsInRole(Roles.Admin))
            {
                return Forbid();
            }

            var stream = _fileStorageService.OpenRead(document.FilePath);
            return File(stream, document.ContentType, document.FileName);
        }

        private async Task<Models.Applications.Application?> GetOwnedApplicationAsync(int id, ApplicationType? expectedType = null)
        {
            var application = await _applicationService.GetByIdAsync(id);
            if (application is null || application.ApplicantUserId != CurrentUserId)
            {
                return null;
            }

            if (expectedType.HasValue && application.ApplicationType != expectedType.Value)
            {
                return null;
            }

            return application;
        }

        private List<ApplicationType> GetAvailableTypes()
        {
            var types = new List<ApplicationType>();
            if (User.IsInRole(Roles.Institute))
            {
                types.Add(ApplicationType.Institute);
            }

            if (User.IsInRole(Roles.QAB))
            {
                types.Add(ApplicationType.QAB);
            }

            return types;
        }
    }
}
