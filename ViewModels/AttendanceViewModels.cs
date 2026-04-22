using MedyxHMS.Models;

// Purpose: Contains application code for AttendanceViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class AttendanceIndexViewModel
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public string StaffIdFilter { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
        public List<StaffAttendance> AttendanceRecords { get; set; } = new();
        public Dictionary<string, int> Summary { get; set; } = new();
        public StaffAttendance ManualAttendance { get; set; } = new StaffAttendance { AttendanceDate = DateTime.Today, Status = "Present" };
    }
}
