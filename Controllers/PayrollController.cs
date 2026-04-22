using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for PayrollController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff")]
    public class PayrollController : Controller
    {
        private readonly IPayrollService _payrollService;
        private readonly IStaffService _staffService;
        private readonly IAuditService _auditService;

        public PayrollController(IPayrollService payrollService, IStaffService staffService, IAuditService auditService)
        {
            _payrollService = payrollService;
            _staffService = staffService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? month = null, string staffId = null)
        {
            var selectedMonth = month.HasValue
                ? new DateTime(month.Value.Year, month.Value.Month, 1)
                : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
            var records = await _payrollService.GetPayrollRecordsAsync(selectedMonth, staffId);

            var viewModel = new PayrollIndexViewModel
            {
                SelectedMonth = selectedMonth,
                StaffIdFilter = staffId,
                StaffOptions = staff,
                PayrollRecords = records.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Generate()
        {
            var staff = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
            return View(new PayrollGenerateViewModel { StaffOptions = staff });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Generate(PayrollGenerateViewModel model)
        {
            try
            {
                await _payrollService.GeneratePayrollAsync(model.StaffId, model.PayrollMonth, model.Allowances, model.Deductions, model.Notes);

                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "CREATE",
                    "PayrollRecord",
                    model.StaffId,
                    null,
                    $"Month: {model.PayrollMonth:yyyy-MM}, Allowances: {model.Allowances}, Deductions: {model.Deductions}");

                TempData["SuccessMessage"] = "Payroll generated successfully.";
                return RedirectToAction(nameof(Index), new { month = model.PayrollMonth.ToString("yyyy-MM-01") });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model.StaffOptions = (await _staffService.GetAllStaffAsync()).Where(s => s.IsActive).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> MarkPaid(int id, DateTime? paymentDate = null, string notes = null)
        {
            var paid = await _payrollService.MarkPayrollAsPaidAsync(id, paymentDate ?? DateTime.UtcNow, notes);
            if (!paid)
            {
                TempData["ErrorMessage"] = "Payroll record not found.";
                return RedirectToAction(nameof(Index));
            }

            await _auditService.LogActivityAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                "UPDATE",
                "PayrollRecord",
                id.ToString(),
                null,
                "Marked payroll as paid");

            TempData["SuccessMessage"] = "Payroll marked as paid.";
            return RedirectToAction(nameof(Index));
        }
    }
}
