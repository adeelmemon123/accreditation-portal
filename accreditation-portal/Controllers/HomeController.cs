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
                return RedirectToAction(nameof(AdminController.Users), "Admin");
            }

            if (User.IsInRole(Roles.Institute) || User.IsInRole(Roles.QAB))
            {
                return RedirectToAction(nameof(ApplicationsController.Index), "Applications");
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
