namespace MedyxHMS.Services.Implementations
{
    /// <summary>Role lists for [Authorize(Roles = ...)] (compile-time constants).</summary>
    public static class AppRoles
    {
        /// <summary>Every staff role (everything except the patient-portal role "Patient").</summary>
        public const string Staff = "SuperAdmin,Admin,Doctor,Nurse,Receptionist,Accountant,Pharmacist,LabTechnician,Radiologist,Pathologist,Staff";

        public const string Managers = "SuperAdmin,Admin";
    }
}
