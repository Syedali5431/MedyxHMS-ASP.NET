using MedyxHMS.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MedyxHMS.ViewModels
{
    // OPD (Outpatient Department) and IPD (Inpatient Department) ViewModels

    public class OPDVisitViewModel
    {
        public OPDVisitDto Visit { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<OPDVisitDto> Visits { get; set; } = new();
        public string Filter { get; set; } = "all";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
    }

    public class OPDVisitDetailsViewModel
    {
        public OPDVisitDto Visit { get; set; }
        public PatientPortalDto Patient { get; set; }
        public StaffDto Doctor { get; set; }
    }

    public class CreateOPDVisitViewModel
    {
        public OPDVisitCreateDto Visit { get; set; } = new();
        public List<MedyxHMS.DTOs.PatientDto> Patients { get; set; } = new();
        public List<StaffDto> Doctors { get; set; } = new();
        public int? SelectedPatientId { get; set; }
        public int? SelectedDoctorId { get; set; }
    }

    public class EditOPDVisitViewModel
    {
        public int VisitId { get; set; }
        public OPDVisitUpdateDto Visit { get; set; } = new();
        public OPDVisitDto CurrentVisit { get; set; }
        public PatientPortalDto Patient { get; set; }
        public StaffDto Doctor { get; set; }
    }

    public class IPDAdmissionViewModel
    {
        public IPDAdmissionDto Admission { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<IPDAdmissionDto> Admissions { get; set; } = new();
        public string Filter { get; set; } = "all";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public string Status { get; set; }
    }

    public class IPDAdmissionDetailsViewModel
    {
        public IPDAdmissionDto Admission { get; set; }
        public PatientPortalDto Patient { get; set; }
        public StaffDto Doctor { get; set; }
        public BedDto Bed { get; set; }
        public WardDto Ward { get; set; }
        public List<BillDto> RelatedBills { get; set; } = new();
        public decimal TotalCharges { get; set; }
    }

    public class CreateIPDAdmissionViewModel
    {
        public IPDAdmissionCreateDto Admission { get; set; } = new();
        public List<MedyxHMS.DTOs.PatientDto> Patients { get; set; } = new();
        public List<StaffDto> Doctors { get; set; } = new();
        public List<BedDto> AvailableBeds { get; set; } = new();
        public List<WardDto> Wards { get; set; } = new();
        public int? SelectedPatientId { get; set; }
        public int? SelectedDoctorId { get; set; }
        public int? SelectedBedId { get; set; }
    }

    public class EditIPDAdmissionViewModel
    {
        public int AdmissionId { get; set; }
        public IPDAdmissionUpdateDto Admission { get; set; } = new();
        public IPDAdmissionDto CurrentAdmission { get; set; }
        public PatientPortalDto Patient { get; set; }
        public StaffDto Doctor { get; set; }
        public List<BedDto> AvailableBeds { get; set; } = new();
        public List<WardDto> Wards { get; set; } = new();
    }

    public class WardViewModel
    {
        public WardDto Ward { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<WardDto> Wards { get; set; } = new();
        public string Filter { get; set; } = "all";
    }

    public class WardDetailsViewModel
    {
        public WardDto Ward { get; set; }
        public List<BedDto> Beds { get; set; } = new();
        public List<IPDAdmissionDto> CurrentAdmissions { get; set; } = new();
        public int AvailableBeds => Ward?.AvailableBeds ?? 0;
        public int OccupiedBeds => Ward?.OccupiedBeds ?? 0;
    }

    public class CreateWardViewModel
    {
        public WardCreateDto Ward { get; set; } = new();
    }

    public class EditWardViewModel
    {
        public int WardId { get; set; }
        public WardUpdateDto Ward { get; set; } = new();
        public WardDto CurrentWard { get; set; }
    }

    public class BedViewModel
    {
        public BedDto Bed { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalRecords { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<BedDto> Beds { get; set; } = new();
        public string Filter { get; set; } = "all";
        public int? WardId { get; set; }
        public string Status { get; set; }
        public string BedType { get; set; }
    }

    public class BedDetailsViewModel
    {
        public BedDto Bed { get; set; }
        public WardDto Ward { get; set; }
        public IPDAdmissionDto CurrentAdmission { get; set; }
        public List<IPDAdmissionDto> AdmissionHistory { get; set; } = new();
    }

    public class CreateBedViewModel
    {
        public BedCreateDto Bed { get; set; } = new();
        public List<WardDto> Wards { get; set; } = new();
        public int? SelectedWardId { get; set; }
    }

    public class EditBedViewModel
    {
        public int BedId { get; set; }
        public BedUpdateDto Bed { get; set; } = new();
        public BedDto CurrentBed { get; set; }
        public List<WardDto> Wards { get; set; } = new();
    }

    // Dashboard ViewModels for OPD/IPD
    public class OPDDashboardViewModel
    {
        public int TodayVisits { get; set; }
        public int ThisWeekVisits { get; set; }
        public int ThisMonthVisits { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal ThisWeekRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public List<OPDVisitDto> RecentVisits { get; set; } = new();
        public List<StaffDto> TopDoctors { get; set; } = new();
        public Dictionary<string, int> VisitsByDepartment { get; set; } = new();
    }

    public class IPDDashboardViewModel
    {
        public int CurrentAdmissions { get; set; }
        public int AvailableBeds { get; set; }
        public int TotalBeds { get; set; }
        public int TodayAdmissions { get; set; }
        public int TodayDischarges { get; set; }
        public decimal AverageStayDuration { get; set; }
        public List<IPDAdmissionDto> RecentAdmissions { get; set; } = new();
        public List<IPDAdmissionDto> RecentDischarges { get; set; } = new();
        public List<WardDto> WardOccupancy { get; set; } = new();
        public Dictionary<string, int> AdmissionsByDepartment { get; set; } = new();
    }
}