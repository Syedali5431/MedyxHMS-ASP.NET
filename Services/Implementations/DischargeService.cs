using System.Globalization;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

// Purpose: Discharging in-patients and their discharge reports. A report is created when the patient is discharged,
// pre-filled from the admission (diagnosis, treatment, operations and test results during the stay), completed by a
// doctor or nurse and downloaded as a PDF with the hospital letterhead.
namespace MedyxHMS.Services.Implementations
{
    public class DischargeService : IDischargeService
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private readonly ApplicationDbContext _db;
        private readonly IIPDService _ipd;
        private readonly IExportService _export;

        public DischargeService(ApplicationDbContext db, IIPDService ipd, IExportService export)
        {
            _db = db;
            _ipd = ipd;
            _export = export;
        }

        private IQueryable<IPDAdmission> Admissions => _db.IPDAdmissions
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Bed).ThenInclude(b => b.Ward);

        private IQueryable<DischargeSummary> Summaries => _db.DischargeSummaries
            .Include(s => s.Patient)
            .Include(s => s.Admission).ThenInclude(a => a.Bed).ThenInclude(b => b.Ward)
            .Include(s => s.Admission).ThenInclude(a => a.Doctor);

        public async Task<DischargeFormViewModel?> NewFormAsync(int admissionId)
        {
            var admission = await Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId);
            if (admission == null) return null;
            var form = new DischargeFormViewModel
            {
                AdmissionId = admission.Id,
                DischargeDate = DateTime.Now,
                ConditionAtDischarge = "Improved"
            };
            await PrefillAsync(form, admission, DateTime.Now);
            return form;
        }

        public async Task<DischargeSummary> DischargeAsync(DischargeFormViewModel form, string? userId, string userName)
        {
            var admission = await Admissions.FirstOrDefaultAsync(a => a.Id == form.AdmissionId)
                ?? throw new InvalidOperationException("The admission was not found.");

            var existing = await _db.DischargeSummaries.FirstOrDefaultAsync(s => s.IPDAdmissionId == admission.Id);
            if (existing != null) return existing;

            if (admission.Status != "Discharged")
            {
                var dischargeDate = form.DischargeDate == default ? DateTime.Now : form.DischargeDate;
                if (dischargeDate < admission.AdmissionDate) dischargeDate = admission.AdmissionDate;
                if (!await _ipd.DischargePatientAsync(admission.Id, dischargeDate))
                {
                    throw new InvalidOperationException("The patient could not be discharged.");
                }
                admission.DischargeDate = dischargeDate;
            }

            var summary = new DischargeSummary
            {
                HospitalId = admission.HospitalId,
                IPDAdmissionId = admission.Id,
                PatientId = admission.PatientId,
                AdmissionDate = admission.AdmissionDate,
                DischargeDate = admission.DischargeDate ?? form.DischargeDate,
                CreatedByUserId = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.Now
            };
            Apply(summary, form);
            if (form.Complete) MarkCompleted(summary, userName);
            _db.DischargeSummaries.Add(summary);
            await _db.SaveChangesAsync();
            return summary;
        }

        public async Task<DischargeSummary?> EnsureForAdmissionAsync(int admissionId, string? userId, string userName)
        {
            var existing = await _db.DischargeSummaries.FirstOrDefaultAsync(s => s.IPDAdmissionId == admissionId);
            if (existing != null) return existing;

            var admission = await Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId);
            if (admission == null || admission.Status != "Discharged") return null;

            var form = new DischargeFormViewModel { AdmissionId = admissionId, DischargeDate = admission.DischargeDate ?? DateTime.Now };
            await PrefillAsync(form, admission, form.DischargeDate);
            return await DischargeAsync(form, userId, userName);
        }

        public Task<DischargeSummary?> GetAsync(int id) => Summaries.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        public Task<DischargeSummary?> GetForAdmissionAsync(int admissionId) =>
            _db.DischargeSummaries.AsNoTracking().FirstOrDefaultAsync(s => s.IPDAdmissionId == admissionId);

        public async Task<DischargeFormViewModel?> EditFormAsync(int id)
        {
            var s = await GetAsync(id);
            if (s == null) return null;
            return new DischargeFormViewModel
            {
                AdmissionId = s.IPDAdmissionId,
                SummaryId = s.Id,
                ReportNumber = s.ReportNumber,
                PatientName = PatientName(s.Patient),
                PatientCode = s.Patient?.PatientId ?? string.Empty,
                WardBed = WardBed(s.Admission),
                AdmissionDate = s.AdmissionDate,
                DischargeDate = s.DischargeDate,
                ConditionAtDischarge = s.ConditionAtDischarge,
                ReasonForAdmission = s.ReasonForAdmission,
                FinalDiagnosis = s.FinalDiagnosis,
                HospitalCourse = s.HospitalCourse,
                ProceduresPerformed = s.ProceduresPerformed,
                InvestigationsSummary = s.InvestigationsSummary,
                DischargeMedications = s.DischargeMedications,
                AdviceOnDischarge = s.AdviceOnDischarge,
                FollowUpInstructions = s.FollowUpInstructions,
                FollowUpDate = s.FollowUpDate,
                AttendingDoctor = s.AttendingDoctor
            };
        }

        public async Task<DischargeSummary?> UpdateAsync(DischargeFormViewModel form, string userName)
        {
            if (!form.SummaryId.HasValue) return null;
            var summary = await _db.DischargeSummaries.FirstOrDefaultAsync(s => s.Id == form.SummaryId.Value);
            if (summary == null || summary.IsCompleted) return null;

            Apply(summary, form);
            summary.UpdatedAt = DateTime.Now;
            if (form.Complete) MarkCompleted(summary, userName);
            await _db.SaveChangesAsync();
            return summary;
        }

        public async Task<bool> ReopenAsync(int id)
        {
            var summary = await _db.DischargeSummaries.FirstOrDefaultAsync(s => s.Id == id);
            if (summary == null || !summary.IsCompleted) return false;
            summary.Status = DischargeSummary.StatusDraft;
            summary.CompletedAt = null;
            summary.CompletedByName = null;
            summary.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<DischargeSummary>> ListAsync(int? patientId, string? search, DateTime? from, DateTime? to, string? status)
        {
            var query = Summaries.AsNoTracking();
            if (patientId.HasValue) query = query.Where(s => s.PatientId == patientId.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(s => (s.Patient.FirstName + " " + s.Patient.LastName).Contains(term)
                                         || s.Patient.PatientId.Contains(term)
                                         || s.Patient.Phone.Contains(term));
            }
            if (from.HasValue) query = query.Where(s => s.DischargeDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(s => s.DischargeDate <= MedyxHMS.Extensions.DateRange.EndOfDay(to.Value));
            if (status is DischargeSummary.StatusDraft or DischargeSummary.StatusCompleted) query = query.Where(s => s.Status == status);
            return await query.OrderByDescending(s => s.DischargeDate).ThenByDescending(s => s.Id).Take(500).ToListAsync();
        }

        public Task<List<DischargeSummary>> ListForPatientAsync(int patientId) =>
            Summaries.AsNoTracking().Where(s => s.PatientId == patientId).OrderByDescending(s => s.DischargeDate).ToListAsync();

        public Task<List<IPDAdmission>> AdmittedAsync() =>
            Admissions.AsNoTracking().Where(a => a.Status == "Admitted").OrderBy(a => a.AdmissionDate).ToListAsync();

        public async Task<List<SelectListItem>> PatientOptionsAsync(int? selectedPatientId)
        {
            // Patients with a discharge report, or currently admitted.
            var withReports = _db.DischargeSummaries.Select(s => s.PatientId);
            var admitted = _db.IPDAdmissions.Where(a => a.Status == "Admitted").Select(a => a.PatientId);
            var patients = await _db.Patients.AsNoTracking()
                .Where(p => withReports.Contains(p.Id) || admitted.Contains(p.Id))
                .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                .Select(p => new { p.Id, p.FirstName, p.LastName, p.PatientId })
                .ToListAsync();
            return patients.Select(p => new SelectListItem($"{p.FirstName} {p.LastName} ({p.PatientId})", p.Id.ToString(Inv), p.Id == selectedPatientId)).ToList();
        }

        public ReportDocument BuildDocument(DischargeSummary s)
        {
            var patient = s.Patient;
            var stayDays = Math.Max(1, (int)Math.Ceiling((s.DischargeDate - s.AdmissionDate).TotalDays));
            var doc = new ReportDocument
            {
                Key = "DS",
                Title = "Discharge Summary",
                Category = "Clinical",
                FilterKind = ReportFilterKind.None,
                PeriodText = $"No. {s.ReportNumber}"
            };
            _export.FillLetterhead(doc, s.HospitalId);
            doc.FooterNote = $"{s.ReportNumber} · Confidential patient information";

            doc.Metrics.Add(new ReportMetric("Admitted", s.AdmissionDate, ReportColumnKind.Date, "info"));
            doc.Metrics.Add(new ReportMetric("Discharged", s.DischargeDate, ReportColumnKind.Date, "success"));
            doc.Metrics.Add(new ReportMetric("Length of stay (days)", stayDays));
            doc.Metrics.Add(new ReportMetric("Condition", s.ConditionAtDischarge, ReportColumnKind.Text, s.ConditionAtDischarge is "Expired" or "Discharged against medical advice" ? "danger" : "success"));

            doc.Sections.Add(new ReportSection
            {
                Title = "Patient",
                Compact = true,
                Columns = { new("Name", ReportColumnKind.Text, 1.5f), new("Patient ID"), new("Age / gender"), new("Blood group", ReportColumnKind.Text, 0.8f), new("Phone"), new("Allergies", ReportColumnKind.Text, 1.3f) },
                Rows = { new object?[] { PatientName(patient), patient?.PatientId, AgeGender(patient), patient?.BloodGroup, patient?.Phone, string.IsNullOrWhiteSpace(patient?.Allergies) ? "None recorded" : patient!.Allergies } }
            });
            doc.Sections.Add(new ReportSection
            {
                Title = "Admission",
                Compact = true,
                Columns = { new("Ward / bed", ReportColumnKind.Text, 1.3f), new("Admission type", ReportColumnKind.Text, 0.9f), new("Attending doctor", ReportColumnKind.Text, 1.6f), new("Admitted", ReportColumnKind.DateTime, 1.1f), new("Discharged", ReportColumnKind.DateTime, 1.1f) },
                Rows = { new object?[] { WardBed(s.Admission), s.Admission?.AdmissionType, s.AttendingDoctor, s.AdmissionDate, s.DischargeDate } }
            });

            var clinical = new ReportSection
            {
                Title = "Clinical summary",
                Compact = true,
                Columns = { new("Item", ReportColumnKind.Text, 1.2f), new("Details", ReportColumnKind.Text, 4.8f) },
                EmptyText = "No clinical details recorded yet."
            };
            void Add(string label, string? text, DateTime? date = null)
            {
                var value = date.HasValue ? date.Value.ToString("dddd, dd-MMM-yyyy", Inv) + (string.IsNullOrWhiteSpace(text) ? "" : " – " + text) : text;
                if (!string.IsNullOrWhiteSpace(value)) clinical.Rows.Add(new object?[] { label, value });
            }
            Add("Reason for admission", s.ReasonForAdmission);
            Add("Final diagnosis", s.FinalDiagnosis);
            Add("Course in hospital", s.HospitalCourse);
            Add("Procedures / operations", s.ProceduresPerformed);
            Add("Investigations", s.InvestigationsSummary);
            Add("Medicines on discharge", s.DischargeMedications);
            Add("Advice", s.AdviceOnDischarge);
            Add("Follow-up", s.FollowUpInstructions, s.FollowUpDate);
            doc.Sections.Add(clinical);

            doc.Notes.Add(s.IsCompleted
                ? $"Completed by {s.CompletedByName} on {s.CompletedAt?.ToString("dd-MMM-yyyy hh:mm tt", Inv)}. Prepared by {s.CreatedByName}."
                : $"DRAFT – not yet completed. Prepared by {s.CreatedByName} on {s.CreatedAt.ToString("dd-MMM-yyyy hh:mm tt", Inv)}.");
            doc.Notes.Add("Bring this summary to your follow-up visit. In an emergency come to the hospital's emergency department.");
            return doc;
        }

        // ── helpers ──────────────────────────────────────────────

        private async Task PrefillAsync(DischargeFormViewModel form, IPDAdmission admission, DateTime until)
        {
            form.PatientName = PatientName(admission.Patient);
            form.PatientCode = admission.Patient?.PatientId ?? string.Empty;
            form.WardBed = WardBed(admission);
            form.AdmissionDate = admission.AdmissionDate;
            form.AttendingDoctor = admission.Doctor == null ? string.Empty : $"Dr. {admission.Doctor.FirstName} {admission.Doctor.LastName}".Trim()
                + (string.IsNullOrWhiteSpace(admission.Doctor.Specialization) ? "" : $" ({admission.Doctor.Specialization})");
            form.ReasonForAdmission = string.Join(" – ", new[] { string.IsNullOrWhiteSpace(admission.AdmissionType) ? null : admission.AdmissionType + " admission", NullIfEmpty(admission.Notes) }.Where(x => x != null));
            form.FinalDiagnosis = NullIfEmpty(admission.Diagnosis);
            form.HospitalCourse = NullIfEmpty(admission.Treatment);

            // Operations and tests during the stay.
            var from = admission.AdmissionDate.Date;
            var to = MedyxHMS.Extensions.DateRange.EndOfDay(until > admission.AdmissionDate ? until : DateTime.Now);
            var operations = await _db.OTSchedules.AsNoTracking()
                .Where(o => o.PatientId == admission.PatientId && o.ScheduledDate >= from && o.ScheduledDate <= to && o.Status != "Cancelled")
                .OrderBy(o => o.ScheduledDate)
                .Select(o => new { o.ProcedureName, o.ScheduledDate, o.Status, Surgeon = o.Surgeon != null ? "Dr. " + o.Surgeon.FirstName + " " + o.Surgeon.LastName : o.SurgeonName })
                .ToListAsync();
            form.ProceduresPerformed = operations.Count == 0 ? null : string.Join("\n", operations.Select(o =>
                $"{o.ProcedureName} – {o.ScheduledDate.ToString("dd-MMM-yyyy", Inv)}{(string.IsNullOrWhiteSpace(o.Surgeon) ? "" : ", " + o.Surgeon)} ({o.Status})"));

            var labs = await _db.LabResults.AsNoTracking()
                .Where(l => l.PatientId == admission.PatientId && l.OrderDate >= from && l.OrderDate <= to && l.Status != "Cancelled")
                .OrderBy(l => l.OrderDate)
                .Select(l => new { l.LabTest.TestName, l.OrderDate, l.ResultValue, l.Unit, l.Interpretation, l.Status })
                .ToListAsync();
            var scans = await _db.RadiologyResults.AsNoTracking()
                .Where(r => r.PatientId == admission.PatientId && r.OrderDate >= from && r.OrderDate <= to && r.Status != "Cancelled")
                .OrderBy(r => r.OrderDate)
                .Select(r => new { r.RadiologyTest.TestName, r.OrderDate, r.Impression, r.Status })
                .ToListAsync();
            var lines = labs.Select(l => $"{l.TestName} ({l.OrderDate.ToString("dd-MMM", Inv)}): "
                    + (string.IsNullOrWhiteSpace(l.ResultValue) ? l.Status : $"{l.ResultValue} {l.Unit}".Trim() + (string.IsNullOrWhiteSpace(l.Interpretation) ? "" : $" – {l.Interpretation}")))
                .Concat(scans.Select(r => $"{r.TestName} ({r.OrderDate.ToString("dd-MMM", Inv)}): " + (string.IsNullOrWhiteSpace(r.Impression) ? r.Status : r.Impression)))
                .ToList();
            form.InvestigationsSummary = lines.Count == 0 ? null : string.Join("\n", lines);
        }

        private static void Apply(DischargeSummary s, DischargeFormViewModel f)
        {
            s.ConditionAtDischarge = string.IsNullOrWhiteSpace(f.ConditionAtDischarge) ? "Improved" : f.ConditionAtDischarge.Trim();
            s.ReasonForAdmission = Clean(f.ReasonForAdmission);
            s.FinalDiagnosis = Clean(f.FinalDiagnosis);
            s.HospitalCourse = Clean(f.HospitalCourse);
            s.ProceduresPerformed = Clean(f.ProceduresPerformed);
            s.InvestigationsSummary = Clean(f.InvestigationsSummary);
            s.DischargeMedications = Clean(f.DischargeMedications);
            s.AdviceOnDischarge = Clean(f.AdviceOnDischarge);
            s.FollowUpInstructions = Clean(f.FollowUpInstructions);
            s.FollowUpDate = f.FollowUpDate?.Date;
            s.AttendingDoctor = Clean(f.AttendingDoctor);
        }

        private static void MarkCompleted(DischargeSummary s, string userName)
        {
            s.Status = DischargeSummary.StatusCompleted;
            s.CompletedByName = userName;
            s.CompletedAt = DateTime.Now;
        }

        private static string Clean(string? text) => (text ?? string.Empty).Trim();
        private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static string PatientName(Patient? p) => p == null ? string.Empty : $"{p.FirstName} {p.LastName}".Trim();

        private static string WardBed(IPDAdmission? a) =>
            a?.Bed == null ? "–" : $"{a.Bed.Ward?.Name} / bed {a.Bed.BedNumber}".Trim(' ', '/');

        private static string AgeGender(Patient? p)
        {
            if (p == null) return string.Empty;
            var age = string.Empty;
            if (p.DateOfBirth > new DateTime(1900, 1, 1) && p.DateOfBirth < DateTime.Today)
            {
                var years = DateTime.Today.Year - p.DateOfBirth.Year;
                if (p.DateOfBirth.Date > DateTime.Today.AddYears(-years)) years--;
                age = years + " years";
            }
            return string.Join(" / ", new[] { age, p.Gender }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}
