using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

// Purpose: Keeps the patient's medical records (shown to staff and in the patient portal) in step with
// OPD visits and IPD admissions: one record per visit/admission, created, updated and removed with its source.
namespace MedyxHMS.Services.Implementations
{
    public class MedicalRecordService : IMedicalRecordService
    {
        public const string OpdSource = "OPDVisit", IpdSource = "IPDAdmission";
        private readonly ApplicationDbContext _context;

        public MedicalRecordService(ApplicationDbContext context)
        {
            _context = context;
        }

        public static string TypeLabel(string recordType) => recordType switch
        {
            "OPD" => "OPD consultation",
            "IPD" => "Inpatient admission",
            _ => recordType
        };

        public async Task SyncOpdVisitAsync(int visitId)
        {
            var visit = await _context.OPDVisits.IgnoreQueryFilters().AsNoTracking().Include(v => v.Doctor).FirstOrDefaultAsync(v => v.Id == visitId);
            var record = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.SourceType == OpdSource && m.SourceId == visitId);
            if (visit == null)
            {
                if (record != null) { _context.MedicalRecords.Remove(record); await _context.SaveChangesAsync(); }
                return;
            }

            var logins = await DoctorLoginsAsync(new[] { visit.Doctor });
            if (record == null) { record = NewRecord(OpdSource, visitId, visit.CreatedBy); _context.MedicalRecords.Add(record); }
            else record.ModifiedDate = DateTime.Now;
            ApplyOpd(record, visit, logins);
            await _context.SaveChangesAsync();
        }

        public async Task SyncIpdAdmissionAsync(int admissionId)
        {
            var admission = await _context.IPDAdmissions.IgnoreQueryFilters().AsNoTracking().Include(a => a.Doctor).FirstOrDefaultAsync(a => a.Id == admissionId);
            var record = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.SourceType == IpdSource && m.SourceId == admissionId);
            if (admission == null)
            {
                if (record != null) { _context.MedicalRecords.Remove(record); await _context.SaveChangesAsync(); }
                return;
            }

            var logins = await DoctorLoginsAsync(new[] { admission.Doctor });
            if (record == null) { record = NewRecord(IpdSource, admissionId, admission.CreatedBy); _context.MedicalRecords.Add(record); }
            else record.ModifiedDate = DateTime.Now;
            ApplyIpd(record, admission, logins);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveForSourceAsync(string sourceType, int sourceId)
        {
            var records = await _context.MedicalRecords.Where(m => m.SourceType == sourceType && m.SourceId == sourceId).ToListAsync();
            _context.MedicalRecords.RemoveRange(records);
        }

        public async Task<int> EnsureRecordsAsync(int? patientId = null)
        {
            var visits = await _context.OPDVisits.IgnoreQueryFilters().AsNoTracking().Include(v => v.Doctor)
                .Where(v => (patientId == null || v.PatientId == patientId)
                            && !_context.MedicalRecords.Any(m => m.SourceType == OpdSource && m.SourceId == v.Id))
                .ToListAsync();
            var admissions = await _context.IPDAdmissions.IgnoreQueryFilters().AsNoTracking().Include(a => a.Doctor)
                .Where(a => (patientId == null || a.PatientId == patientId)
                            && !_context.MedicalRecords.Any(m => m.SourceType == IpdSource && m.SourceId == a.Id))
                .ToListAsync();
            if (visits.Count == 0 && admissions.Count == 0)
                return 0;

            var logins = await DoctorLoginsAsync(visits.Select(v => v.Doctor).Concat(admissions.Select(a => a.Doctor)));
            var added = new List<MedicalRecord>();
            foreach (var visit in visits)
            {
                var record = NewRecord(OpdSource, visit.Id, visit.CreatedBy);
                ApplyOpd(record, visit, logins);
                added.Add(record);
            }
            foreach (var admission in admissions)
            {
                var record = NewRecord(IpdSource, admission.Id, admission.CreatedBy);
                ApplyIpd(record, admission, logins);
                added.Add(record);
            }

            _context.MedicalRecords.AddRange(added);
            try
            {
                await _context.SaveChangesAsync();
                return added.Count;
            }
            catch (DbUpdateException)
            {
                // Another request created the same records at the same moment (unique source index): keep theirs.
                foreach (var record in added) _context.Entry(record).State = EntityState.Detached;
                return 0;
            }
        }

        private static MedicalRecord NewRecord(string sourceType, int sourceId, string? createdBy) => new()
        {
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedDate = DateTime.Now,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "System" : createdBy
        };

        private static void ApplyOpd(MedicalRecord record, OPDVisit visit, IReadOnlyDictionary<int, string> logins)
        {
            var symptoms = visit.Symptoms?.Trim();
            var prescription = visit.Prescription?.Trim();
            record.PatientId = visit.PatientId;
            record.RecordType = "OPD";
            record.RecordDate = visit.VisitDate;
            record.Description = Cut(string.IsNullOrEmpty(symptoms) ? "OPD consultation" : $"OPD consultation – {symptoms}", 500);
            record.Diagnosis = Cut(visit.Diagnosis, 1000);
            record.Treatment = Cut(string.Join(Environment.NewLine, new[] { visit.Treatment?.Trim(), string.IsNullOrEmpty(prescription) ? null : "Prescribed: " + prescription }
                .Where(x => !string.IsNullOrEmpty(x))), 2000);
            record.Notes = Cut(visit.Notes, 1000);
            ApplyDoctor(record, visit.Doctor, logins);
        }

        private static void ApplyIpd(MedicalRecord record, IPDAdmission admission, IReadOnlyDictionary<int, string> logins)
        {
            // While admitted, a discharge date is only the expected one.
            var stillAdmitted = string.Equals(admission.Status, "Admitted", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(admission.Status);
            var stay = stillAdmitted
                ? "currently admitted" + (admission.DischargeDate.HasValue ? $", expected discharge {admission.DischargeDate.Value:dd-MMM-yyyy}" : string.Empty)
                : string.Equals(admission.Status, "Discharged", StringComparison.OrdinalIgnoreCase)
                    ? "discharged" + (admission.DischargeDate.HasValue ? $" {admission.DischargeDate.Value:dd-MMM-yyyy}" : string.Empty)
                    : admission.Status.Trim().ToLowerInvariant() + (admission.DischargeDate.HasValue ? $" {admission.DischargeDate.Value:dd-MMM-yyyy}" : string.Empty);
            var type = string.IsNullOrWhiteSpace(admission.AdmissionType) ? "Admission" : $"{admission.AdmissionType.Trim()} admission";
            record.PatientId = admission.PatientId;
            record.RecordType = "IPD";
            record.RecordDate = admission.AdmissionDate;
            record.Description = Cut($"{type} on {admission.AdmissionDate:dd-MMM-yyyy}, {stay}", 500);
            record.Diagnosis = Cut(admission.Diagnosis, 1000);
            record.Treatment = Cut(admission.Treatment, 2000);
            record.Notes = Cut(admission.Notes, 1000);
            ApplyDoctor(record, admission.Doctor, logins);
        }

        private static void ApplyDoctor(MedicalRecord record, Doctor? doctor, IReadOnlyDictionary<int, string> logins)
        {
            record.DoctorName = Cut(doctor != null ? $"{doctor.FirstName} {doctor.LastName}".Trim() : string.Empty, 100);
            record.DoctorId = doctor != null && logins.TryGetValue(doctor.Id, out var userId) ? userId : null;
        }

        /// <summary>Logins of doctors that have one (matched on employee id or e-mail).</summary>
        private async Task<Dictionary<int, string>> DoctorLoginsAsync(IEnumerable<Doctor?> doctors)
        {
            var list = doctors.Where(d => d != null).Select(d => d!).DistinctBy(d => d.Id).ToList();
            var employeeIds = list.Select(d => d.EmployeeId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var emails = list.Select(d => d.Email).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (employeeIds.Count == 0 && emails.Count == 0)
                return new Dictionary<int, string>();

            var users = await _context.Users.AsNoTracking()
                .Where(u => employeeIds.Contains(u.EmployeeId) || emails.Contains(u.Email!))
                .Select(u => new { u.Id, u.EmployeeId, u.Email })
                .ToListAsync();
            var result = new Dictionary<int, string>();
            foreach (var doctor in list)
            {
                var user = users.FirstOrDefault(u => !string.IsNullOrWhiteSpace(doctor.EmployeeId) && u.EmployeeId == doctor.EmployeeId)
                           ?? users.FirstOrDefault(u => !string.IsNullOrWhiteSpace(doctor.Email) && string.Equals(u.Email, doctor.Email, StringComparison.OrdinalIgnoreCase));
                if (user != null) result[doctor.Id] = user.Id;
            }
            return result;
        }

        private static string Cut(string? value, int max)
        {
            var text = value?.Trim() ?? string.Empty;
            return text.Length <= max ? text : text[..max];
        }
    }
}
