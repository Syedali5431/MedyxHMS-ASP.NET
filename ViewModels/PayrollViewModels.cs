using MedyxHMS.Models;

// Purpose: Contains application code for PayrollViewModels and its related runtime behavior.
namespace MedyxHMS.ViewModels
{
    public class PayrollIndexViewModel
    {
        public DateTime SelectedMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public string StaffIdFilter { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
        public List<PayrollRecord> PayrollRecords { get; set; } = new();
    }

    public class PayrollGenerateViewModel
    {
        public string StaffId { get; set; }
        public DateTime PayrollMonth { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public string Notes { get; set; }
        public List<Staff> StaffOptions { get; set; } = new();
    }
}
