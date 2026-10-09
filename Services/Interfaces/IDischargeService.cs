using MedyxHMS.Models;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

// Purpose: Discharging in-patients and their discharge reports (create, complete, list, PDF document).
namespace MedyxHMS.Services.Interfaces
{
    public interface IDischargeService
    {
        /// <summary>Discharge Patient form for an admitted patient, pre-filled from the admission. Null if not found.</summary>
        Task<DischargeFormViewModel?> NewFormAsync(int admissionId);

        /// <summary>Discharges the patient (bed, IPD bill, medical record) and creates the discharge report.</summary>
        Task<DischargeSummary> DischargeAsync(DischargeFormViewModel form, string? userId, string userName);

        /// <summary>The report of an admission that was discharged elsewhere (e.g. status changed on the edit page); created if missing.</summary>
        Task<DischargeSummary?> EnsureForAdmissionAsync(int admissionId, string? userId, string userName);

        Task<DischargeSummary?> GetAsync(int id);
        Task<DischargeSummary?> GetForAdmissionAsync(int admissionId);
        Task<DischargeFormViewModel?> EditFormAsync(int id);

        /// <summary>Saves an edited draft (and completes it when form.Complete is set). Null if not found or already completed.</summary>
        Task<DischargeSummary?> UpdateAsync(DischargeFormViewModel form, string userName);

        /// <summary>Back to draft so a completed report can be corrected (Admin, SuperAdmin).</summary>
        Task<bool> ReopenAsync(int id);

        Task<List<DischargeSummary>> ListAsync(int? patientId, string? search, DateTime? from, DateTime? to, string? status);
        Task<List<DischargeSummary>> ListForPatientAsync(int patientId);
        Task<List<IPDAdmission>> AdmittedAsync();
        Task<List<SelectListItem>> PatientOptionsAsync(int? selectedPatientId);

        /// <summary>The report as a document (letterhead and logo) for PDF download and printing.</summary>
        ReportDocument BuildDocument(DischargeSummary summary);
    }
}
