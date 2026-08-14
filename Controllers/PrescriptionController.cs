using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Purpose: Contains application code for PrescriptionController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Doctor,Nurse,Staff,Pharmacist")]
    public class PrescriptionController : Controller
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService _patientService;
        private readonly IBillingService _billingService;
        private readonly IAuditService _auditService;

        public PrescriptionController(
            IPrescriptionService prescriptionService,
            IPatientService patientService,
            IBillingService billingService,
            IAuditService auditService)
        {
            _prescriptionService = prescriptionService;
            _patientService = patientService;
            _billingService = billingService;
            _auditService = auditService;
        }

        #region Prescriptions

        // List all prescriptions
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? patientId = null, string? search = null, DateTime? from = null, DateTime? to = null)
        {
            var prescriptions = await _prescriptionService.GetAllPrescriptionsAsync();

            if (patientId.HasValue)
            {
                prescriptions = prescriptions.Where(p => p.PharmacyBill.PatientId == patientId.Value).ToList();
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                prescriptions = prescriptions.Where(p =>
                    (p.Medicine?.Name != null && p.Medicine.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.PharmacyBill?.Patient?.FirstName != null && p.PharmacyBill.Patient.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (p.PharmacyBill?.Patient?.LastName != null && p.PharmacyBill.Patient.LastName.Contains(search, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            if (from.HasValue)
                prescriptions = prescriptions.Where(p => p.CreatedDate >= from.Value).ToList();
            if (to.HasValue)
                prescriptions = prescriptions.Where(p => p.CreatedDate <= to.Value.AddDays(1).AddTicks(-1)).ToList();

            var prescriptionList = prescriptions
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new PrescriptionListViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = prescriptions.Count(),
                PatientId = patientId,
                Search = search,
                FromDate = from,
                ToDate = to,
                Prescriptions = prescriptionList.Select(p => new PrescriptionDto
                {
                    Id = p.Id,
                    PharmacyBillId = p.PharmacyBillId,
                    MedicineId = p.MedicineId,
                    MedicineName = p.Medicine?.Name,
                    Dosage = p.Dosage,
                    Frequency = p.Frequency,
                    Duration = p.Duration,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice,
                    TotalPrice = p.TotalPrice,
                    Instructions = p.Instructions,
                    CreatedDate = p.CreatedDate
                }).ToList()
            };

            return View(viewModel);
        }

        // Get prescription details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var prescription = await _prescriptionService.GetPrescriptionByIdAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            var dto = new PrescriptionDto
            {
                Id = prescription.Id,
                PharmacyBillId = prescription.PharmacyBillId,
                MedicineId = prescription.MedicineId,
                MedicineName = prescription.Medicine?.Name,
                Dosage = prescription.Dosage,
                Frequency = prescription.Frequency,
                Duration = prescription.Duration,
                Quantity = prescription.Quantity,
                UnitPrice = prescription.UnitPrice,
                TotalPrice = prescription.TotalPrice,
                Instructions = prescription.Instructions,
                CreatedDate = prescription.CreatedDate
            };

            return View(dto);
        }

        // Get prescription create form
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Create(int? pharmacyBillId)
        {
            var medicines = await _prescriptionService.GetAllMedicinesAsync();

            var viewModel = new CreatePrescriptionViewModel
            {
                Prescription = new PrescriptionCreateDto
                {
                    PharmacyBillId = pharmacyBillId ?? 0,
                    Duration = 7
                },
                Medicines = medicines.Select(m => new MedicineDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    DosageForm = m.DosageForm,
                    Strength = m.Strength,
                    UnitPrice = m.UnitPrice,
                    StockQuantity = m.StockQuantity
                }).ToList(),
                SelectedPharmacyBillId = pharmacyBillId
            };

            return View(viewModel);
        }

        // Post prescription create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Create(CreatePrescriptionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var prescription = new Prescription
                {
                    PharmacyBillId = model.Prescription.PharmacyBillId,
                    MedicineId = model.Prescription.MedicineId,
                    Dosage = model.Prescription.Dosage,
                    Frequency = model.Prescription.Frequency,
                    Duration = model.Prescription.Duration,
                    Quantity = model.Prescription.Quantity,
                    UnitPrice = model.Prescription.UnitPrice,
                    TotalPrice = model.Prescription.Quantity * model.Prescription.UnitPrice,
                    Instructions = model.Prescription.Instructions
                };

                await _prescriptionService.CreatePrescriptionAsync(prescription);

                // Log activity
                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "Prescription",
                    prescription.Id.ToString(),
                    null,
                    $"Medicine: {prescription.MedicineId}, Dosage: {prescription.Dosage}, Quantity: {prescription.Quantity}"
                );

                TempData["Success"] = "Prescription created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var medicines = await _prescriptionService.GetAllMedicinesAsync();
            model.Medicines = medicines.Select(m => new MedicineDto
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                UnitPrice = m.UnitPrice
            }).ToList();

            return View(model);
        }

        // Delete prescription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _prescriptionService.DeletePrescriptionAsync(id);
            if (result)
            {
                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "DELETE",
                    "Prescription",
                    id.ToString(),
                    "",
                    ""
                );

                TempData["Success"] = "Prescription deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete prescription.";
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Medicines

        // List all medicines
        [HttpGet]
        public async Task<IActionResult> Medicines(int page = 1, int pageSize = 10, string search = "")
        {
            var medicines = await _prescriptionService.GetAllMedicinesAsync();

            if (!string.IsNullOrEmpty(search))
            {
                medicines = medicines.Where(m => m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                               m.GenericName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var medicineList = medicines
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new MedicineListViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = medicines.Count(),
                SearchTerm = search,
                Medicines = medicineList.Select(m => new MedicineDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    Category = m.Category,
                    DosageForm = m.DosageForm,
                    Strength = m.Strength,
                    Manufacturer = m.Manufacturer,
                    UnitPrice = m.UnitPrice,
                    StockQuantity = m.StockQuantity,
                    MinStockLevel = m.MinStockLevel,
                    ExpiryDate = m.ExpiryDate,
                    BatchNumber = m.BatchNumber,
                    IsActive = m.IsActive,
                    CreatedDate = m.CreatedDate
                }).ToList()
            };

            return View(viewModel);
        }

        // Get medicine details
        [HttpGet]
        public async Task<IActionResult> MedicineDetails(int id)
        {
            var medicine = await _prescriptionService.GetMedicineByIdAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }

            var dto = new MedicineDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                GenericName = medicine.GenericName,
                Category = medicine.Category,
                DosageForm = medicine.DosageForm,
                Strength = medicine.Strength,
                Manufacturer = medicine.Manufacturer,
                UnitPrice = medicine.UnitPrice,
                StockQuantity = medicine.StockQuantity,
                MinStockLevel = medicine.MinStockLevel,
                ExpiryDate = medicine.ExpiryDate,
                BatchNumber = medicine.BatchNumber,
                IsActive = medicine.IsActive,
                CreatedDate = medicine.CreatedDate
            };

            return View(dto);
        }

        // Get medicine create form
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Pharmacist")]
        public IActionResult CreateMedicine()
        {
            return View(new MedicineCreateDto { IsActive = true });
        }

        // Post medicine create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Pharmacist")]
        public async Task<IActionResult> CreateMedicine(MedicineCreateDto model)
        {
            if (ModelState.IsValid)
            {
                var medicine = new Medicine
                {
                    Name = model.Name,
                    GenericName = model.GenericName,
                    Category = model.Category,
                    DosageForm = model.DosageForm,
                    Strength = model.Strength,
                    Manufacturer = model.Manufacturer,
                    UnitPrice = model.UnitPrice,
                    StockQuantity = model.StockQuantity,
                    MinStockLevel = model.MinStockLevel,
                    ExpiryDate = model.ExpiryDate,
                    BatchNumber = model.BatchNumber,
                    IsActive = model.IsActive
                };

                await _prescriptionService.CreateMedicineAsync(medicine);

                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "Medicine",
                    medicine.Id.ToString(),
                    null,
                    $"Name: {medicine.Name}, StockQuantity: {medicine.StockQuantity}"
                );

                TempData["Success"] = "Medicine created successfully.";
                return RedirectToAction(nameof(Medicines));
            }

            return View(model);
        }

        // Get medicine edit form
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Pharmacist")]
        public async Task<IActionResult> EditMedicine(int id)
        {
            var medicine = await _prescriptionService.GetMedicineByIdAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }

            var model = new MedicineUpdateDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Category = medicine.Category,
                UnitPrice = medicine.UnitPrice,
                StockQuantity = medicine.StockQuantity,
                MinStockLevel = medicine.MinStockLevel,
                ExpiryDate = medicine.ExpiryDate,
                IsActive = medicine.IsActive
            };

            return View(model);
        }

        // Post medicine edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Pharmacist")]
        public async Task<IActionResult> EditMedicine(MedicineUpdateDto model)
        {
            var medicine = await _prescriptionService.GetMedicineByIdAsync(model.Id);
            if (medicine == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    medicine.Name = model.Name;
                if (!string.IsNullOrEmpty(model.Category))
                    medicine.Category = model.Category;
                if (model.UnitPrice.HasValue)
                    medicine.UnitPrice = model.UnitPrice.Value;
                if (model.StockQuantity.HasValue)
                    medicine.StockQuantity = model.StockQuantity.Value;
                if (model.MinStockLevel.HasValue)
                    medicine.MinStockLevel = model.MinStockLevel.Value;
                if (model.ExpiryDate.HasValue)
                    medicine.ExpiryDate = model.ExpiryDate.Value;
                if (model.IsActive.HasValue)
                    medicine.IsActive = model.IsActive.Value;

                await _prescriptionService.UpdateMedicineAsync(medicine);

                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "UPDATE",
                    "Medicine",
                    medicine.Id.ToString(),
                    "",
                    $"Name: {medicine.Name}, StockQuantity: {medicine.StockQuantity}"
                );

                TempData["Success"] = "Medicine updated successfully.";
                return RedirectToAction(nameof(Medicines));
            }

            return View(model);
        }

        // AJAX: Get low stock medicines
        [HttpGet]
        public async Task<IActionResult> GetLowStockMedicines()
        {
            var medicines = await _prescriptionService.GetLowStockMedicinesAsync();
            return Json(new
            {
                success = true,
                medicines = medicines.Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    stockQuantity = m.StockQuantity,
                    minStockLevel = m.MinStockLevel
                }).ToList()
            });
        }

        // AJAX: Get expiring medicines
        [HttpGet]
        public async Task<IActionResult> GetExpiringMedicines(int daysAhead = 30)
        {
            var medicines = await _prescriptionService.GetExpiringMedicinesAsync(daysAhead);
            return Json(new
            {
                success = true,
                medicines = medicines.Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    expiryDate = m.ExpiryDate.ToString("yyyy-MM-dd"),
                    daysRemaining = (m.ExpiryDate.Date - DateTime.UtcNow.Date).Days
                }).ToList()
            });
        }

        #endregion

        #region ViewModels

        public class PrescriptionListViewModel
        {
            public List<PrescriptionDto> Prescriptions { get; set; } = new();
            public int CurrentPage { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public int TotalRecords { get; set; }
            public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
            public bool HasPreviousPage => CurrentPage > 1;
            public bool HasNextPage => CurrentPage < TotalPages;
            public int? PatientId { get; set; }
            public string? Search { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
        }

        public class CreatePrescriptionViewModel
        {
            public PrescriptionCreateDto Prescription { get; set; } = new();
            public List<MedicineDto> Medicines { get; set; } = new();
            public int? SelectedPharmacyBillId { get; set; }
        }

        public class MedicineListViewModel
        {
            public List<MedicineDto> Medicines { get; set; } = new();
            public int CurrentPage { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public int TotalRecords { get; set; }
            public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
            public bool HasPreviousPage => CurrentPage > 1;
            public bool HasNextPage => CurrentPage < TotalPages;
            public string SearchTerm { get; set; } = string.Empty;
        }

        #endregion
    }
}
