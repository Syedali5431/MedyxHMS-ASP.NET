using MedyxHMS.Services.Interfaces;

namespace MedyxHMS.Services.Implementations
{
    /// <summary>Per-request holder for the active hospital. Defaults to "no filter" until the middleware fills it.</summary>
    public class HospitalContext : IHospitalContext
    {
        public bool FilterEnabled { get; set; }
        public int? ActiveHospitalId { get; set; }
        public int? HospitalIdForNewRecords { get; set; }
        public int? DefaultHospitalId { get; set; }
        public IReadOnlyList<HospitalOption> AccessibleHospitals { get; set; } = Array.Empty<HospitalOption>();
        public IReadOnlyList<HospitalOption> AllHospitals { get; set; } = Array.Empty<HospitalOption>();
        public bool CanViewAllHospitals { get; set; }
        public bool IsStaffContext { get; set; }

        public string ActiveHospitalName => ActiveHospitalId.HasValue ? HospitalName(ActiveHospitalId) : "All hospitals";

        public string HospitalName(int? hospitalId) =>
            hospitalId.HasValue ? AllHospitals.FirstOrDefault(h => h.Id == hospitalId.Value)?.Name ?? string.Empty : string.Empty;

        public bool ShowHospitalColumn => IsStaffContext && !FilterEnabled && AllHospitals.Count > 1;
    }
}
