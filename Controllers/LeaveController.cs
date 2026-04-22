using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// Purpose: Contains application code for LeaveController and its related runtime behavior.
namespace MedyxHMS.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin,Staff,Nurse,Doctor")]
    public class LeaveController : Controller
    {
        private readonly ILeaveService _leaveService;
        private readonly IStaffService _staffService;
        private readonly IAuditService _auditService;

        public LeaveController(ILeaveService leaveService, IStaffService staffService, IAuditService auditService)
        {
            _leaveService = leaveService;
            _staffService = staffService;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, string staffId = null, string status = null)
        {
            var requests = await _leaveService.GetLeaveRequestsAsync(staffId, status, startDate, endDate);
            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();

            var viewModel = new LeaveIndexViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                StaffIdFilter = staffId,
                StatusFilter = status,
                StaffOptions = staff,
                LeaveRequests = requests.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Request()
        {
            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
            var leaveTypes = (await _leaveService.GetLeaveTypesAsync(activeOnly: true)).ToList();

            var viewModel = new LeaveRequestCreateViewModel
            {
                StaffOptions = staff,
                LeaveTypes = leaveTypes
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(LeaveRequestCreateViewModel model)
        {
            if (model?.LeaveRequest == null)
            {
                TempData["ErrorMessage"] = "Invalid leave request.";
                return RedirectToAction(nameof(Request));
            }

            try
            {
                if (string.IsNullOrWhiteSpace(model.LeaveRequest.StaffId))
                {
                    model.LeaveRequest.StaffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                var created = await _leaveService.CreateLeaveRequestAsync(model.LeaveRequest);
                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "CREATE",
                    "LeaveRequest",
                    created.Id.ToString(),
                    null,
                    $"Staff: {created.StaffId}, Type: {created.LeaveTypeId}, Days: {created.TotalDays}");

                TempData["SuccessMessage"] = "Leave request submitted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                model.StaffOptions = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();
                model.LeaveTypes = (await _leaveService.GetLeaveTypesAsync(activeOnly: true)).ToList();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string remarks)
        {
            try
            {
                var success = await _leaveService.UpdateLeaveRequestStatusAsync(
                    id,
                    status,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    remarks);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Leave request not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _auditService.LogActivityAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    "UPDATE",
                    "LeaveRequest",
                    id.ToString(),
                    null,
                    $"Status changed to {status}");

                TempData["SuccessMessage"] = "Leave request status updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Types()
        {
            var viewModel = new LeaveTypeViewModel
            {
                LeaveTypes = (await _leaveService.GetLeaveTypesAsync()).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateType(LeaveTypeViewModel model)
        {
            try
            {
                await _leaveService.CreateLeaveTypeAsync(model.NewLeaveType);
                TempData["SuccessMessage"] = "Leave type created successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Types));
        }

        [HttpGet]
        public async Task<IActionResult> Balances(int year = 0, string staffId = null)
        {
            var selectedYear = year == 0 ? DateTime.Today.Year : year;
            var balances = await _leaveService.GetLeaveBalancesAsync(staffId, selectedYear);
            var staff = (await _staffService.GetAllStaffAsync()).OrderBy(s => s.FirstName).ThenBy(s => s.LastName).ToList();

            var viewModel = new LeaveBalanceViewModel
            {
                Year = selectedYear,
                StaffIdFilter = staffId,
                StaffOptions = staff,
                Balances = balances.ToList()
            };

            return View(viewModel);
        }
    }
}
