using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor,Nurse")]
    public class IPDController : Controller
    {
        private readonly IIPDService _ipdService;
        private readonly IPatientService _patientService;
        private readonly IStaffService _staffService;
        private readonly IWardService _wardService;
        private readonly IBedService _bedService;
        private readonly IBillingService _billingService;
        private readonly IAuditService _auditService;

        public IPDController(
            IIPDService ipdService,
            IPatientService patientService,
            IStaffService staffService,
            IWardService wardService,
            IBedService bedService,
            IBillingService billingService,
            IAuditService auditService)
        {
            _ipdService = ipdService;
            _patientService = patientService;
            _staffService = staffService;
            _wardService = wardService;
            _bedService = bedService;
            _billingService = billingService;
            _auditService = auditService;
        }

        // List all IPD admissions
        [HttpGet]
        public async Task<IActionResult> Index(string status = "all", int page = 1, int pageSize = 10,
            DateTime? startDate = null, DateTime? endDate = null, int? doctorId = null, int? patientId = null)
        {
            var admissions = await _ipdService.GetAllIPDAdmissionsAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                admissions = admissions.Where(a => string.Equals(a.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (startDate.HasValue)
            {
                admissions = admissions.Where(a => a.AdmissionDate >= startDate.Value.Date).ToList();
            }

            if (endDate.HasValue)
            {
                admissions = admissions.Where(a => a.AdmissionDate <= endDate.Value.Date.AddDays(1).AddTicks(-1)).ToList();
            }

            if (doctorId.HasValue)
            {
                admissions = admissions.Where(a => a.DoctorId == doctorId.Value).ToList();
            }

            if (patientId.HasValue)
            {
                admissions = admissions.Where(a => a.PatientId == patientId.Value).ToList();
            }

            var admissionList = admissions
                .OrderByDescending(a => a.AdmissionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new IPDAdmissionViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = admissions.Count(),
                Status = status,
                StartDate = startDate,
                EndDate = endDate,
                DoctorId = doctorId,
                PatientId = patientId,
                Admissions = admissionList.Select(a => MapToIPDAdmissionDto(a)).ToList()
            };

            return View(viewModel);
        }

        // Get IPD admission details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var admission = await _ipdService.GetIPDAdmissionByIdAsync(id);
            if (admission == null)
            {
                return NotFound();
            }

            var patient = await _patientService.GetPatientByIdAsync(admission.PatientId);
            var doctor = await _staffService.GetStaffByIdAsync(admission.DoctorId.ToString());
            var relatedBills = await _billingService.GetBillsByPatientAsync(admission.PatientId);
            var ipdBills = relatedBills.Where(b => b.BillType == "IPD").ToList();

            var totalCharges = admission.DailyCharges * ((admission.DischargeDate ?? DateTime.UtcNow) - admission.AdmissionDate).TotalDays;

            var viewModel = new IPDAdmissionDetailsViewModel
            {
                Admission = MapToIPDAdmissionDto(admission),
                Patient = MapToPatientPortalDto(patient),
                Doctor = MapToStaffDto(doctor),
                Bed = admission.Bed != null ? MapToBedDto(admission.Bed) : null,
                Ward = admission.Bed?.Ward != null ? MapToWardDto(admission.Bed.Ward) : null,
                RelatedBills = ipdBills.Select(b => MapToBillDto(b)).ToList(),
                TotalCharges = (decimal)totalCharges
            };

            return View(viewModel);
        }

        // Get IPD admission create form
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Create(int? patientId)
        {
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _staffService.GetAllStaffAsync();
            var wards = await _wardService.GetAllWardsAsync();
            var availableBeds = await _bedService.GetAvailableBedsAsync();

            var viewModel = new CreateIPDAdmissionViewModel
            {
                Admission = new IPDAdmissionCreateDto
                {
                    AdmissionDate = DateTime.Now,
                    PatientId = patientId ?? 0,
                    Status = "Admitted",
                    AdmissionType = "Planned"
                },
                Patients = patients.Select(p => new PatientDto
                {
                    Id = p.Id,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = p.Email,
                    Phone = p.Phone
                }).ToList(),
                Doctors = doctors.Where(d => d.Department == "Doctor" || d.Department == "Physician").Select(d => new StaffDto
                {
                    Id = d.Id,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    EmployeeId = d.EmployeeId,
                    Department = d.Department
                }).ToList(),
                Wards = wards.Select(w => MapToWardDto(w)).ToList(),
                AvailableBeds = availableBeds.Select(b => MapToBedDto(b)).ToList(),
                SelectedPatientId = patientId
            };

            return View(viewModel);
        }

        // Post IPD admission create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Create(CreateIPDAdmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admission = new IPDAdmission
                {
                    PatientId = model.Admission.PatientId,
                    DoctorId = model.Admission.DoctorId,
                    BedId = model.Admission.BedId,
                    AdmissionDate = model.Admission.AdmissionDate,
                    AdmissionType = model.Admission.AdmissionType,
                    Diagnosis = model.Admission.Diagnosis,
                    Treatment = model.Admission.Treatment,
                    Notes = model.Admission.Notes,
                    DailyCharges = model.Admission.DailyCharges,
                    Status = "Admitted",
                    CreatedBy = User.Identity.Name
                };

                await _ipdService.CreateIPDAdmissionAsync(admission);

                // Log activity
                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "CREATE",
                    "IPDAdmission",
                    admission.Id.ToString(),
                    null,
                    $"Patient: {admission.PatientId}, Doctor: {admission.DoctorId}, Diagnosis: {admission.Diagnosis}"
                );

                TempData["Success"] = "IPD admission created successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdown data if validation fails
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _staffService.GetAllStaffAsync();
            var wards = await _wardService.GetAllWardsAsync();
            var availableBeds = await _bedService.GetAvailableBedsAsync();

            model.Patients = patients.Select(p => new PatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone
            }).ToList();

            model.Doctors = doctors.Where(d => d.Department == "Doctor" || d.Department == "Physician").Select(d => new StaffDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName,
                EmployeeId = d.EmployeeId,
                Department = d.Department
            }).ToList();

            model.Wards = wards.Select(w => MapToWardDto(w)).ToList();
            model.AvailableBeds = availableBeds.Select(b => MapToBedDto(b)).ToList();

            return View(model);
        }

        // Get IPD admission edit form
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Edit(int id)
        {
            var admission = await _ipdService.GetIPDAdmissionByIdAsync(id);
            if (admission == null)
            {
                return NotFound();
            }

            var patient = await _patientService.GetPatientByIdAsync(admission.PatientId);
            var doctor = await _staffService.GetStaffByIdAsync(admission.DoctorId.ToString());
            var availableBeds = await _bedService.GetAvailableBedsAsync();
            var wards = await _wardService.GetAllWardsAsync();

            var viewModel = new EditIPDAdmissionViewModel
            {
                AdmissionId = admission.Id,
                Admission = new IPDAdmissionUpdateDto
                {
                    Id = admission.Id,
                    BedId = admission.BedId,
                    DischargeDate = admission.DischargeDate,
                    Diagnosis = admission.Diagnosis,
                    Treatment = admission.Treatment,
                    Notes = admission.Notes,
                    Status = admission.Status
                },
                CurrentAdmission = MapToIPDAdmissionDto(admission),
                Patient = MapToPatientPortalDto(patient),
                Doctor = MapToStaffDto(doctor),
                AvailableBeds = availableBeds.Select(b => MapToBedDto(b)).ToList(),
                Wards = wards.Select(w => MapToWardDto(w)).ToList()
            };

            return View(viewModel);
        }

        // Post IPD admission edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor,Staff")]
        public async Task<IActionResult> Edit(EditIPDAdmissionViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingAdmission = await _ipdService.GetIPDAdmissionByIdAsync(model.AdmissionId);
                if (existingAdmission == null)
                {
                    return NotFound();
                }

                var oldValues = $"Status: {existingAdmission.Status}, Bed: {existingAdmission.BedId}";

                existingAdmission.BedId = model.Admission.BedId ?? existingAdmission.BedId;
                existingAdmission.Diagnosis = model.Admission.Diagnosis ?? existingAdmission.Diagnosis;
                existingAdmission.Treatment = model.Admission.Treatment ?? existingAdmission.Treatment;
                existingAdmission.Notes = model.Admission.Notes ?? existingAdmission.Notes;
                existingAdmission.Status = model.Admission.Status ?? existingAdmission.Status;

                if (model.Admission.DischargeDate.HasValue)
                {
                    existingAdmission.DischargeDate = model.Admission.DischargeDate;
                }

                await _ipdService.UpdateIPDAdmissionAsync(existingAdmission);

                // Log activity
                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "UPDATE",
                    "IPDAdmission",
                    existingAdmission.Id.ToString(),
                    oldValues,
                    $"Status: {existingAdmission.Status}, Diagnosis: {existingAdmission.Diagnosis}"
                );

                TempData["Success"] = "IPD admission updated successfully.";
                return RedirectToAction(nameof(Details), new { id = model.AdmissionId });
            }

            var admission = await _ipdService.GetIPDAdmissionByIdAsync(model.AdmissionId);
            if (admission != null)
            {
                var patient = await _patientService.GetPatientByIdAsync(admission.PatientId);
                var doctor = await _staffService.GetStaffByIdAsync(admission.DoctorId.ToString());
                var availableBeds = await _bedService.GetAvailableBedsAsync();
                var wards = await _wardService.GetAllWardsAsync();

                model.CurrentAdmission = MapToIPDAdmissionDto(admission);
                model.Patient = MapToPatientPortalDto(patient);
                model.Doctor = MapToStaffDto(doctor);
                model.AvailableBeds = availableBeds.Select(b => MapToBedDto(b)).ToList();
                model.Wards = wards.Select(w => MapToWardDto(w)).ToList();
            }

            return View(model);
        }

        // Discharge patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin,Doctor")]
        public async Task<IActionResult> Discharge(int id, DateTime dischargeDate)
        {
            var admission = await _ipdService.GetIPDAdmissionByIdAsync(id);
            if (admission == null)
            {
                return NotFound();
            }

            var result = await _ipdService.DischargePatientAsync(id, dischargeDate);
            if (result)
            {
                // Calculate and create discharge bill
                var daysAdmitted = (dischargeDate.Date - admission.AdmissionDate.Date).TotalDays;
                var totalCharges = admission.DailyCharges * (decimal)daysAdmitted;

                // Log activity
                await _auditService.LogActivityAsync(
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    "DISCHARGE",
                    "IPDAdmission",
                    admission.Id.ToString(),
                    $"Status: Admitted",
                    $"Status: Discharged, Total Charges: {totalCharges}"
                );

                TempData["Success"] = $"Patient discharged successfully. Total charges: {totalCharges:C}";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Error"] = "Failed to discharge patient.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // AJAX: Get available beds for ward
        [HttpGet]
        public async Task<IActionResult> GetAvailableBedsForWard(int wardId)
        {
            var beds = await _bedService.GetBedsByWardAsync(wardId);
            var availableBeds = beds.Where(b => b.Status == "Available").ToList();

            return Json(new
            {
                success = true,
                beds = availableBeds.Select(b => new
                {
                    id = b.Id,
                    bedNumber = b.BedNumber,
                    bedType = b.BedType,
                    dailyCharges = b.DailyCharges
                }).ToList()
            });
        }

        // AJAX: Get patient medical history
        [HttpGet]
        public async Task<IActionResult> GetPatientMedicalHistory(int patientId)
        {
            var previousAdmissions = await _ipdService.GetIPDAdmissionsByPatientAsync(patientId);
            var previousAdmissionsList = previousAdmissions.Where(a => a.Status == "Discharged").Take(5).ToList();

            return Json(new
            {
                success = true,
                admissions = previousAdmissionsList.Select(a => new
                {
                    admissionDate = a.AdmissionDate.ToString("yyyy-MM-dd"),
                    dischargeDate = a.DischargeDate?.ToString("yyyy-MM-dd"),
                    diagnosis = a.Diagnosis,
                    treatment = a.Treatment
                }).ToList()
            });
        }

        #region Helper Methods

        private IPDAdmissionDto MapToIPDAdmissionDto(IPDAdmission admission)
        {
            return new IPDAdmissionDto
            {
                Id = admission.Id,
                PatientId = admission.PatientId,
                PatientName = admission.Patient != null ? $"{admission.Patient.FirstName} {admission.Patient.LastName}" : "Unknown",
                DoctorId = admission.DoctorId,
                DoctorName = admission.Doctor != null ? $"{admission.Doctor.FirstName} {admission.Doctor.LastName}" : "Unknown",
                BedId = admission.BedId,
                BedNumber = admission.Bed?.BedNumber,
                WardName = admission.Bed?.Ward?.Name,
                AdmissionDate = admission.AdmissionDate,
                DischargeDate = admission.DischargeDate,
                AdmissionType = admission.AdmissionType,
                Diagnosis = admission.Diagnosis,
                Treatment = admission.Treatment,
                Notes = admission.Notes,
                Status = admission.Status,
                DailyCharges = admission.DailyCharges,
                CreatedDate = admission.CreatedDate,
                CreatedBy = admission.CreatedBy
            };
        }

        private PatientPortalDto MapToPatientPortalDto(Patient patient)
        {
            if (patient == null) return null;
            return new PatientPortalDto
            {
                Id = patient.Id.ToString(),
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                Email = patient.Email,
                Phone = patient.Phone,
                DateOfBirth = patient.DateOfBirth,
                Gender = patient.Gender,
                Address = patient.Address
            };
        }

        private StaffDto MapToStaffDto(Staff staff)
        {
            if (staff == null) return null;
            return new StaffDto
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.Email,
                Phone = staff.Phone,
                EmployeeId = staff.EmployeeId,
                Department = staff.Department
            };
        }

        private BedDto MapToBedDto(Bed bed)
        {
            if (bed == null) return null;
            return new BedDto
            {
                Id = bed.Id,
                WardId = bed.WardId,
                WardName = bed.Ward?.Name,
                BedNumber = bed.BedNumber,
                BedType = bed.BedType,
                DailyCharges = bed.DailyCharges,
                Status = bed.Status,
                IsActive = bed.IsActive,
                CreatedDate = bed.CreatedDate
            };
        }

        private WardDto MapToWardDto(Ward ward)
        {
            if (ward == null) return null;
            return new WardDto
            {
                Id = ward.Id,
                Name = ward.Name,
                Description = ward.Description,
                TotalBeds = ward.TotalBeds,
                OccupiedBeds = ward.OccupiedBeds,
                IsActive = ward.IsActive,
                CreatedDate = ward.CreatedDate
            };
        }

        private BillDto MapToBillDto(Bill bill)
        {
            if (bill == null) return null;
            return new BillDto
            {
                Id = bill.Id,
                BillNumber = bill.BillNumber,
                PatientId = bill.PatientId,
                BillDate = bill.BillDate,
                TotalAmount = bill.TotalAmount,
                PaidAmount = bill.PaidAmount,
                PendingAmount = bill.PendingAmount,
                Status = bill.Status,
                BillType = bill.BillType
            };
        }

        #endregion
    }
}
