namespace MedyxHMS.Services.Interfaces
{
    public sealed record HospitalOption(int Id, string Code, string Name, bool IsActive, bool IsDefault, string City);

    /// <summary>
    /// The hospital the current request works in. Filled once per request by HospitalContextMiddleware
    /// and read by ApplicationDbContext (query filters and new-record defaults) and by the layout switcher.
    /// Outside a staff request (patient portal, public site, start-up, background jobs) no filter applies.
    /// </summary>
    public interface IHospitalContext
    {
        /// <summary>True when lists/dashboards must be limited to <see cref="ActiveHospitalId"/>.</summary>
        bool FilterEnabled { get; }

        /// <summary>The selected hospital, or null when viewing all hospitals (or no staff user).</summary>
        int? ActiveHospitalId { get; }

        /// <summary>Hospital given to records created in this request.</summary>
        int? HospitalIdForNewRecords { get; }

        /// <summary>The group's default hospital.</summary>
        int? DefaultHospitalId { get; }

        /// <summary>Hospitals the user may switch to (active only).</summary>
        IReadOnlyList<HospitalOption> AccessibleHospitals { get; }

        /// <summary>All hospitals of the group (active and inactive), used to show hospital names.</summary>
        IReadOnlyList<HospitalOption> AllHospitals { get; }

        /// <summary>True for users allowed to choose "All hospitals" (SuperAdmin).</summary>
        bool CanViewAllHospitals { get; }

        /// <summary>True when this is a staff request where the hospital switcher applies.</summary>
        bool IsStaffContext { get; }

        string ActiveHospitalName { get; }

        /// <summary>Name of a hospital by id ("" when unknown).</summary>
        string HospitalName(int? hospitalId);

        /// <summary>True when lists should show a Hospital column (viewing all hospitals of a multi-hospital group).</summary>
        bool ShowHospitalColumn { get; }
    }
}
