using MedyxHMS.DTOs;
using MedyxHMS.Models;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    public class PatientIndexViewModel
    {
        public IEnumerable<PatientDto> Patients { get; set; }
        public string SearchTerm { get; set; }
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int InactivePatients { get; set; }
    }

    public class PatientCreateViewModel
    {
        public PatientCreateDto Patient { get; set; }

        // Dropdown options
        public List<string> GenderOptions => new List<string> { "Male", "Female", "Other" };
        public List<string> BloodGroupOptions => new List<string>
        {
            "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
        };
        public List<string> CountryOptions => new List<string>
        {
            "United States", "Canada", "United Kingdom", "Australia", "India", "Other"
        };
    }

    public class PatientEditViewModel
    {
        public PatientDto CurrentPatient { get; set; }
        public PatientUpdateDto Patient { get; set; }

        // Dropdown options
        public List<string> GenderOptions => new List<string> { "Male", "Female", "Other" };
        public List<string> BloodGroupOptions => new List<string>
        {
            "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-"
        };
        public List<string> CountryOptions => new List<string>
        {
            "United States", "Canada", "United Kingdom", "Australia", "India", "Other"
        };
    }

    public class PatientDetailsViewModel
    {
        public PatientDto Patient { get; set; }
        public IEnumerable<AppointmentSummaryDto> RecentAppointments { get; set; }
        public IEnumerable<BillSummaryDto> RecentBills { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalBills { get; set; }
        public decimal TotalAmountPaid { get; set; }
    }

    public class PatientDeleteViewModel
    {
        public PatientDto Patient { get; set; }
        public bool HasActiveAppointments { get; set; }
        public bool HasUnpaidBills { get; set; }
        public string WarningMessage { get; set; }
    }

    // Supporting DTOs for related data
    public class AppointmentSummaryDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; }
        public string AppointmentType { get; set; }
        public string DoctorName { get; set; }
    }

    public class BillSummaryDto
    {
        public int Id { get; set; }
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }

    public class PatientDashboardViewModel
    {
        public PatientDto Patient { get; set; }
        public IEnumerable<AppointmentSummaryDto> UpcomingAppointments { get; set; }
        public IEnumerable<BillSummaryDto> PendingBills { get; set; }
        public IEnumerable<MedicalRecord> RecentMedicalRecords { get; set; }
        public IEnumerable<TestResult> RecentTestResults { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalBills { get; set; }
        public decimal TotalAmountDue { get; set; }
        public string WelcomeMessage { get; set; }
        public DateTime CurrentDate { get; set; } = DateTime.Now;
    }
}