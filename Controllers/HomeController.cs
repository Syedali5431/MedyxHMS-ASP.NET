using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MedyxHMS.Models;

// Purpose: Contains application code for HomeController and its related runtime behavior.
namespace MedyxHMS.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// The site root: visitors see the public hospital website, signed-in users go to their own start page
    /// (the page used to be the ASP.NET project template's "Welcome").
    /// </summary>
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Index", "Site");
        }

        var patientOnly = User.IsInRole("Patient") && !new[] { "SuperAdmin", "Admin", "Doctor", "Nurse", "Pharmacist", "Accountant", "Receptionist", "LabTechnician", "Radiologist", "Staff" }.Any(User.IsInRole);
        return LocalRedirect(patientOnly ? "/PatientPortal/Dashboard" : "/Dashboard");
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

    // Re-executed by UseStatusCodePagesWithReExecute for empty 4xx/5xx page responses.
    // The original status code is kept on the response.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        return View(code);
    }
}
