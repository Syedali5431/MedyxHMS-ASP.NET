using MedyxHMS.DTOs;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PatientDto = MedyxHMS.DTOs.PatientDto;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Doctor")]
    public class OPDController : Controller
    {
        private readonly IOPDService _opdService;
        private readonly IPatientService _patientService;
        private readonly IStaffService _staffService;
        private readonly IBillingService _billingService;

        public OPDController(IOPDService opdService, IPatientService patientService, IStaffService staffService, IBillingService billingService)
        {
            _opdService = opdService;
            _patientService = patientService;
            _staffService = staffService;
            _billingService = billingService;
        }

        public async Task<IActionResult> Index(string filter = "all", int page = 1, int pageSize = 10,
            DateTime? startDate = null, DateTime? endDate = null, int? doctorId = null, int? patientId = null)
        {
            var visits = await _opdService.GetAllOPDVisitsAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                visits = visits.Where(v => string.Equals(v.PaymentStatus, filter, StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
            {
                visits = visits.Where(v => v.VisitDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                visits = visits.Where(v => v.VisitDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            if (doctorId.HasValue)
            {
                visits = visits.Where(v => v.DoctorId == doctorId.Value);
            }

            if (patientId.HasValue)
            {
                visits = visits.Where(v => v.PatientId == patientId.Value);
            }

            var visitList = visits
                .OrderByDescending(v => v.VisitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new OPDVisitViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = visits.Count(),
                Filter = filter,
                StartDate = startDate,
                EndDate = endDate,
                DoctorId = doctorId,
                PatientId = patientId,
                Visits = visitList.Select(v => new OPDVisitDto
                {
                    Id = v.Id,
                    PatientId = v.PatientId,
                    PatientName = v.Patient != null ? $"{v.Patient.FirstName} {v.Patient.LastName}" : "Unknown",
                    DoctorId = v.DoctorId,
                    DoctorName = v.Doctor != null ? $"{v.Doctor.FirstName} {v.Doctor.LastName}" : "Unknown",
                    VisitDate = v.VisitDate,
                    Symptoms = v.Symptoms,
                    Diagnosis = v.Diagnosis,
                    Treatment = v.Treatment,
                    Prescription = v.Prescription,
                    Notes = v.Notes,
                    ConsultationFee = v.ConsultationFee,
                    PaymentStatus = v.PaymentStatus,
                    CreatedDate = v.CreatedDate,
                    CreatedBy = v.CreatedBy
                }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var visit = await _opdService.GetOPDVisitByIdAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            var patient = await _patientService.GetPatientByIdAsync(visit.PatientId);
            var doctor = await _staffService.GetStaffByIdAsync(visit.DoctorId.ToString());

            var viewModel = new OPDVisitDetailsViewModel
            {
                Visit = new OPDVisitDto
                {
                    Id = visit.Id,
                    PatientId = visit.PatientId,
                    PatientName = visit.Patient != null ? $"{visit.Patient.FirstName} {visit.Patient.LastName}" : "Unknown",
                    DoctorId = visit.DoctorId,
                    DoctorName = visit.Doctor != null ? $"{visit.Doctor.FirstName} {visit.Doctor.LastName}" : "Unknown",
                    VisitDate = visit.VisitDate,
                    Symptoms = visit.Symptoms,
                    Diagnosis = visit.Diagnosis,
                    Treatment = visit.Treatment,
                    Prescription = visit.Prescription,
                    Notes = visit.Notes,
                    ConsultationFee = visit.ConsultationFee,
                    PaymentStatus = visit.PaymentStatus,
                    CreatedDate = visit.CreatedDate,
                    CreatedBy = visit.CreatedBy
                },
                Patient = patient != null ? new PatientPortalDto
                {
                    Id = patient.Id.ToString(),
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    Address = patient.Address
                } : null,
                Doctor = doctor != null ? new StaffDto
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Email = doctor.Email,
                    Phone = doctor.Phone,
                    EmployeeId = doctor.EmployeeId,
                    Department = doctor.Department
                } : null
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create(int? patientId, int? doctorId)
        {
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _staffService.GetAllStaffAsync();

            var viewModel = new CreateOPDVisitViewModel
            {
                Visit = new OPDVisitCreateDto
                {
                    VisitDate = DateTime.Now,
                    PatientId = patientId ?? 0,
                    DoctorId = doctorId ?? 0
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
                SelectedPatientId = patientId,
                SelectedDoctorId = doctorId
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOPDVisitViewModel model)
        {
            if (ModelState.IsValid)
            {
                var visit = new OPDVisit
                {
                    PatientId = model.Visit.PatientId,
                    DoctorId = model.Visit.DoctorId,
                    VisitDate = model.Visit.VisitDate,
                    Symptoms = model.Visit.Symptoms,
                    Diagnosis = model.Visit.Diagnosis,
                    Treatment = model.Visit.Treatment,
                    Prescription = model.Visit.Prescription,
                    Notes = model.Visit.Notes,
                    ConsultationFee = model.Visit.ConsultationFee,
                    PaymentStatus = model.Visit.PaymentStatus,
                    CreatedBy = User.Identity.Name
                };

                var createdVisit = await _opdService.CreateOPDVisitAsync(visit);

                // Link OPD visit to billing by creating a consultation bill.
                if (createdVisit.ConsultationFee > 0 && !string.Equals(createdVisit.PaymentStatus, "Waived", StringComparison.OrdinalIgnoreCase))
                {
                    var isPaid = string.Equals(createdVisit.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase);
                    var bill = new Bill
                    {
                        PatientId = createdVisit.PatientId,
                        BillDate = createdVisit.VisitDate,
                        DueDate = createdVisit.VisitDate.Date,
                        TotalAmount = createdVisit.ConsultationFee,
                        PaidAmount = isPaid ? createdVisit.ConsultationFee : 0,
                        PendingAmount = isPaid ? 0 : createdVisit.ConsultationFee,
                        Status = isPaid ? "Paid" : "Unpaid",
                        BillType = "OPD",
                        Notes = $"OPD consultation bill for OPD Visit ID: {createdVisit.Id}",
                        CreatedBy = User.Identity?.Name ?? "System",
                        BillItems = new List<BillItem>
                        {
                            new BillItem
                            {
                                ItemName = "OPD Consultation",
                                ItemType = "Service",
                                Quantity = 1,
                                UnitPrice = createdVisit.ConsultationFee,
                                TotalPrice = createdVisit.ConsultationFee,
                                Description = $"Consultation charge for OPD Visit #{createdVisit.Id}",
                                CreatedDate = DateTime.UtcNow
                            }
                        }
                    };

                    var createdBill = await _billingService.CreateBillAsync(bill);
                    TempData["Success"] = createdBill != null
                        ? $"OPD visit created successfully. Bill {createdBill.BillNumber} generated."
                        : "OPD visit created successfully.";
                }
                else
                {
                    TempData["Success"] = "OPD visit created successfully.";
                }

                return RedirectToAction(nameof(Index));
            }

            // Reload dropdown data if validation fails
            var patients = await _patientService.GetAllPatientsAsync();
            var doctors = await _staffService.GetAllStaffAsync();

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

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var visit = await _opdService.GetOPDVisitByIdAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            var patient = await _patientService.GetPatientByIdAsync(visit.PatientId);
            var doctor = await _staffService.GetStaffByIdAsync(visit.DoctorId.ToString());

            var viewModel = new EditOPDVisitViewModel
            {
                VisitId = visit.Id,
                Visit = new OPDVisitUpdateDto
                {
                    Id = visit.Id,
                    Diagnosis = visit.Diagnosis,
                    Treatment = visit.Treatment,
                    Prescription = visit.Prescription,
                    Notes = visit.Notes,
                    PaymentStatus = visit.PaymentStatus
                },
                CurrentVisit = new OPDVisitDto
                {
                    Id = visit.Id,
                    PatientId = visit.PatientId,
                    PatientName = visit.Patient != null ? $"{visit.Patient.FirstName} {visit.Patient.LastName}" : "Unknown",
                    DoctorId = visit.DoctorId,
                    DoctorName = visit.Doctor != null ? $"{visit.Doctor.FirstName} {visit.Doctor.LastName}" : "Unknown",
                    VisitDate = visit.VisitDate,
                    Symptoms = visit.Symptoms,
                    Diagnosis = visit.Diagnosis,
                    Treatment = visit.Treatment,
                    Prescription = visit.Prescription,
                    Notes = visit.Notes,
                    ConsultationFee = visit.ConsultationFee,
                    PaymentStatus = visit.PaymentStatus,
                    CreatedDate = visit.CreatedDate,
                    CreatedBy = visit.CreatedBy
                },
                Patient = patient != null ? new PatientPortalDto
                {
                    Id = patient.Id.ToString(),
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    Address = patient.Address
                } : null,
                Doctor = doctor != null ? new StaffDto
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Email = doctor.Email,
                    Phone = doctor.Phone,
                    EmployeeId = doctor.EmployeeId,
                    Department = doctor.Department
                } : null
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditOPDVisitViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingVisit = await _opdService.GetOPDVisitByIdAsync(model.VisitId);
                if (existingVisit == null)
                {
                    return NotFound();
                }

                existingVisit.Diagnosis = model.Visit.Diagnosis;
                existingVisit.Treatment = model.Visit.Treatment;
                existingVisit.Prescription = model.Visit.Prescription;
                existingVisit.Notes = model.Visit.Notes;
                existingVisit.PaymentStatus = model.Visit.PaymentStatus;

                await _opdService.UpdateOPDVisitAsync(existingVisit);
                TempData["Success"] = "OPD visit updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Reload current visit data if validation fails
            var visit = await _opdService.GetOPDVisitByIdAsync(model.VisitId);
            if (visit != null)
            {
                var patient = await _patientService.GetPatientByIdAsync(visit.PatientId);
                var doctor = await _staffService.GetStaffByIdAsync(visit.DoctorId.ToString());

                model.CurrentVisit = new OPDVisitDto
                {
                    Id = visit.Id,
                    PatientId = visit.PatientId,
                    PatientName = visit.Patient != null ? $"{visit.Patient.FirstName} {visit.Patient.LastName}" : "Unknown",
                    DoctorId = visit.DoctorId,
                    DoctorName = visit.Doctor != null ? $"{visit.Doctor.FirstName} {visit.Doctor.LastName}" : "Unknown",
                    VisitDate = visit.VisitDate,
                    Symptoms = visit.Symptoms,
                    Diagnosis = visit.Diagnosis,
                    Treatment = visit.Treatment,
                    Prescription = visit.Prescription,
                    Notes = visit.Notes,
                    ConsultationFee = visit.ConsultationFee,
                    PaymentStatus = visit.PaymentStatus,
                    CreatedDate = visit.CreatedDate,
                    CreatedBy = visit.CreatedBy
                };

                model.Patient = patient != null ? new PatientPortalDto
                {
                    Id = patient.Id.ToString(),
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email,
                    Phone = patient.Phone,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    Address = patient.Address
                } : null;

                model.Doctor = doctor != null ? new StaffDto
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    Email = doctor.Email,
                    Phone = doctor.Phone,
                    EmployeeId = doctor.EmployeeId,
                    Department = doctor.Department
                } : null;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _opdService.DeleteOPDVisitAsync(id);
            if (result)
            {
                TempData["Success"] = "OPD visit deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to delete OPD visit.";
            }
            return RedirectToAction(nameof(Index));
        }

        // AJAX endpoints for dynamic content
        [HttpGet]
        public async Task<IActionResult> GetPatientDetails(int patientId)
        {
            var patient = await _patientService.GetPatientByIdAsync(patientId);
            if (patient == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                patient = new
                {
                    id = patient.Id,
                    name = $"{patient.FirstName} {patient.LastName}",
                    phone = patient.Phone,
                    email = patient.Email,
                    dateOfBirth = patient.DateOfBirth.ToString("yyyy-MM-dd"),
                    gender = patient.Gender
                }
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorSchedule(int doctorId, DateTime date)
        {
            // This would integrate with appointment system to check doctor's availability
            // For now, return basic availability
            return Json(new { available = true });
        }
    }
}