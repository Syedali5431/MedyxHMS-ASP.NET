using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for CertificateController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    /// <summary>
    /// Certificates & ID Cards module controller.
    /// Accessible to all non-Patient roles: Admin, SuperAdmin, Doctor, Nurse, Pharmacist, Accountant, Receptionist, Staff, LabTechnician, Radiologist.
    /// Patient role is blocked by the [Authorize] gate.
    /// </summary>
    [Authorize(Roles = "Admin,SuperAdmin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Radiologist,Staff")]
    public class CertificateController : Controller
    {
        private readonly ICertificateService _certificateService;
        private readonly IStaffService _staffService;
        private readonly IAuditService _auditService;

        public CertificateController(ICertificateService certificateService, IStaffService staffService, IAuditService auditService)
        {
            _certificateService = certificateService;
            _staffService = staffService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string staffId = null)
        {
            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
            var certificates = await _certificateService.GetCertificatesAsync(staffId);
            var idCards = await _certificateService.GetIdCardsAsync(staffId);

            var viewModel = new CertificateIndexViewModel
            {
                StaffIdFilter = staffId,
                StaffOptions = staff,
                Certificates = certificates.ToList(),
                IdCards = idCards.ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// Birth Certificate landing page and generator.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Birth()
        {
            var viewModel = new CertificateIndexViewModel
            {
                StaffOptions = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList(),
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBirth([FromForm] MedyxHMS.Models.BirthRecord model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return RedirectToAction("Birth");
            }

            if (string.IsNullOrWhiteSpace(model.CertificateNumber))
                model.CertificateNumber = $"BR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            model.CreatedDate = DateTime.UtcNow;
            try
            {
                var db = HttpContext.RequestServices.GetService(typeof(MedyxHMS.Data.ApplicationDbContext)) as MedyxHMS.Data.ApplicationDbContext;
                db.BirthRecords.Add(model);
                await db.SaveChangesAsync();
                await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "CREATE", "BirthRecord", model.Id.ToString(), null,
                    $"Baby: {model.BabyName}, Mother: {model.MotherName}");
                TempData["SuccessMessage"] = "Birth certificate generated.";
                return RedirectToAction("BirthDetails", "BirthDeath", new { id = model.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Birth");
            }
        }


        /// <summary>
        /// Death Certificate landing page and generator.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Death()
        {
            var viewModel = new CertificateIndexViewModel
            {
                StaffOptions = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList(),
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeath([FromForm] MedyxHMS.Models.DeathRecord model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return RedirectToAction("Death");
            }

            if (string.IsNullOrWhiteSpace(model.CertificateNumber))
                model.CertificateNumber = $"DR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            model.CreatedDate = DateTime.UtcNow;
            try
            {
                var db = HttpContext.RequestServices.GetService(typeof(MedyxHMS.Data.ApplicationDbContext)) as MedyxHMS.Data.ApplicationDbContext;
                db.DeathRecords.Add(model);
                await db.SaveChangesAsync();
                await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "CREATE", "DeathRecord", model.Id.ToString(), null,
                    $"Patient: {model.PatientName}, Cause: {model.CauseOfDeath}");
                TempData["SuccessMessage"] = "Death certificate generated.";
                return RedirectToAction("DeathDetails", "BirthDeath", new { id = model.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Death");
            }
        }
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateCertificate()
        {
            return View(new GenerateCertificateViewModel
            {
                StaffOptions = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateCertificate(GenerateCertificateViewModel model)
        {
            try
            {
                model.Certificate.GeneratedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _certificateService.GenerateCertificateAsync(model.Certificate);
                TempData["SuccessMessage"] = "Certificate generated successfully.";
                return RedirectToAction(nameof(Index), new { staffId = model.Certificate.StaffId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model.StaffOptions = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateIdCard()
        {
            return View(new GenerateIdCardViewModel
            {
                StaffOptions = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateIdCard(GenerateIdCardViewModel model)
        {
            try
            {
                await _certificateService.GenerateIdCardAsync(model.IdCard);
                TempData["SuccessMessage"] = "ID card generated successfully.";
                return RedirectToAction(nameof(Index), new { staffId = model.IdCard.StaffId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model.StaffOptions = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
                return View(model);
            }
        }
    }
}
