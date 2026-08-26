using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PatientSurvey.WebUI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DoctorsController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Users", new { area = "Admin", roleId = 3 });
    }
}
