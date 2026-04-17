using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Receptionist")]
    public class FrontOfficeController : Controller
    {
        private readonly IFrontOfficeService _frontOfficeService;
        private readonly IAuditService _auditService;

        public FrontOfficeController(IFrontOfficeService frontOfficeService, IAuditService auditService)
        {
            _frontOfficeService = frontOfficeService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var viewModel = new FrontOfficeDashboardViewModel
            {
                Date = selectedDate,
                Visitors = (await _frontOfficeService.GetVisitorsAsync(selectedDate)).ToList(),
                Complaints = (await _frontOfficeService.GetComplaintsAsync()).Take(10).ToList(),
                DispatchReceiveRecords = (await _frontOfficeService.GetDispatchReceiveRecordsAsync(date: selectedDate)).Take(10).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Visitors(DateTime? date = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var viewModel = new VisitorPageViewModel
            {
                Date = selectedDate,
                Visitors = (await _frontOfficeService.GetVisitorsAsync(selectedDate)).ToList(),
                NewVisitor = { VisitDate = selectedDate }
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVisitor(VisitorPageViewModel model)
        {
            try
            {
                await _frontOfficeService.AddVisitorAsync(model.NewVisitor);
                await _auditService.LogActivityAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), "CREATE", "VisitorLog", "0", null, $"Visitor: {model.NewVisitor.VisitorName}");
                TempData["SuccessMessage"] = "Visitor log added.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Visitors), new { date = model.Date });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutVisitor(int id, DateTime? date = null)
        {
            var success = await _frontOfficeService.CheckOutVisitorAsync(id, DateTime.UtcNow);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Visitor checked out." : "Visitor not found.";
            return RedirectToAction(nameof(Visitors), new { date = date ?? DateTime.Today });
        }

        [HttpGet]
        public async Task<IActionResult> Complaints(string status = null)
        {
            var viewModel = new ComplaintPageViewModel
            {
                StatusFilter = status,
                Complaints = (await _frontOfficeService.GetComplaintsAsync(status)).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComplaint(ComplaintPageViewModel model)
        {
            try
            {
                await _frontOfficeService.AddComplaintAsync(model.NewComplaint);
                TempData["SuccessMessage"] = "Complaint added.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Complaints), new { status = model.StatusFilter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaintStatus(int id, string status, string resolutionNotes)
        {
            var success = await _frontOfficeService.UpdateComplaintStatusAsync(id, status, resolutionNotes);
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Complaint status updated." : "Complaint not found.";
            return RedirectToAction(nameof(Complaints));
        }

        [HttpGet]
        public async Task<IActionResult> DispatchReceive(string recordType = null, DateTime? date = null)
        {
            var viewModel = new DispatchReceivePageViewModel
            {
                RecordTypeFilter = recordType,
                DateFilter = date,
                Records = (await _frontOfficeService.GetDispatchReceiveRecordsAsync(recordType, date)).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDispatchReceive(DispatchReceivePageViewModel model)
        {
            try
            {
                await _frontOfficeService.AddDispatchReceiveRecordAsync(model.NewRecord);
                TempData["SuccessMessage"] = "Record added.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(DispatchReceive), new { recordType = model.RecordTypeFilter, date = model.DateFilter });
        }
    }
}
