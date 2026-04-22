using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for AttendanceController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Nurse,Doctor")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IStaffService _staffService;
        private readonly IAuditService _auditService;

        public AttendanceController(
            IAttendanceService attendanceService,
            IStaffService staffService,
            IAuditService auditService)
        {
            _attendanceService = attendanceService;
            _staffService = staffService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date = null, string staffId = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
            var records = await _attendanceService.GetAttendanceAsync(selectedDate, staffId);
            var summary = await _attendanceService.GetAttendanceSummaryAsync(selectedDate, selectedDate);

            var viewModel = new AttendanceIndexViewModel
            {
                SelectedDate = selectedDate,
                StaffIdFilter = staffId,
                StaffOptions = staff,
                AttendanceRecords = records.ToList(),
                Summary = summary,
                ManualAttendance = new StaffAttendance
                {
                    AttendanceDate = selectedDate,
                    Status = "Present"
                }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAttendance(AttendanceIndexViewModel model)
        {
            try
            {
                if (model?.ManualAttendance == null || string.IsNullOrWhiteSpace(model.ManualAttendance.StaffId))
                {
                    TempData["ErrorMessage"] = "Staff selection is required.";
                    return RedirectToAction(nameof(Index), new { date = model?.SelectedDate ?? DateTime.Today });
                }

                model.ManualAttendance.AttendanceDate = (model.ManualAttendance.AttendanceDate == default ? DateTime.Today : model.ManualAttendance.AttendanceDate).Date;
                await _attendanceService.MarkAttendanceAsync(model.ManualAttendance);

                await _auditService.LogActivityAsync(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    "CREATE_OR_UPDATE",
                    "StaffAttendance",
                    model.ManualAttendance.StaffId,
                    null,
                    $"Date: {model.ManualAttendance.AttendanceDate:yyyy-MM-dd}, Status: {model.ManualAttendance.Status}");

                TempData["SuccessMessage"] = "Attendance saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { date = model?.SelectedDate ?? DateTime.Today, staffId = model?.StaffIdFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(DateTime? date = null)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var checkInTime = DateTime.UtcNow;
                if (date.HasValue)
                {
                    checkInTime = date.Value.Date.Add(checkInTime.TimeOfDay);
                }

                await _attendanceService.CheckInAsync(staffId, checkInTime, "Self check-in");
                TempData["SuccessMessage"] = "Check-in recorded successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { date = (date ?? DateTime.Today).Date });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(DateTime? date = null)
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var checkOutTime = DateTime.UtcNow;
                if (date.HasValue)
                {
                    checkOutTime = date.Value.Date.Add(checkOutTime.TimeOfDay);
                }

                await _attendanceService.CheckOutAsync(staffId, checkOutTime, "Self check-out");
                TempData["SuccessMessage"] = "Check-out recorded successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index), new { date = (date ?? DateTime.Today).Date });
        }
    }
}
