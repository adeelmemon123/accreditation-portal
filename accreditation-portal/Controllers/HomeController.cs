using accreditation_portal.Authorization;
using accreditation_portal.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace accreditation_portal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Signed-in Admin/Institute/QAB have no use for the public marketing home page -
            // send them straight into their sidebar console instead.
            if (User.IsInRole(Roles.Admin))
            {
                return RedirectToAction(nameof(AdminController.Dashboard), "Admin");
            }

            if (User.IsInRole(Roles.Institute) || User.IsInRole(Roles.QAB))
            {
                return RedirectToAction(nameof(ApplicationsController.Index), "Applications");
            }

            // Not signed in at all - skip the marketing page and go straight to the login screen.
            if (User.Identity is not { IsAuthenticated: true })
            {
                return RedirectToAction(nameof(AccountController.Login), "Account");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
