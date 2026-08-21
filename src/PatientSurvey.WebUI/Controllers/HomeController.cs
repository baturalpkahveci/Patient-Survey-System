using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PatientSurvey.WebUI.ErrorHandling;

namespace PatientSurvey.WebUI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Status(int code)
    {
        return code switch
        {
            StatusCodes.Status404NotFound => View("NotFound"),
            StatusCodes.Status403Forbidden => View("AccessDenied"),
            _ => View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier })
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
