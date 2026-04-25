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
        /// Birth Certificate landing page (Phase 1 stub; Phase 2 will add popup form).
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

        /// <summary>
        /// Death Certificate landing page (Phase 1 stub; Phase 2 will add popup form).
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
