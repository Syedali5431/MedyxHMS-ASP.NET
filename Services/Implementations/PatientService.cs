using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Services.Implementations
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;

        public PatientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<Patient?> GetPatientByIdAsync(int id)
        {
            return await _context.Patients
                .Include(p => p.Appointments)
                .Include(p => p.Bills)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            patient.PatientId = GeneratePatientId();
            patient.CreatedDate = DateTime.UtcNow;
            patient.IsActive = true;

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            return patient;
        }

        public async Task<Patient?> UpdatePatientAsync(Patient patient)
        {
            var existingPatient = await _context.Patients.FindAsync(patient.Id);
            if (existingPatient == null)
                return null;

            // Update properties
            existingPatient.FirstName = patient.FirstName;
            existingPatient.LastName = patient.LastName;
            existingPatient.Email = patient.Email;
            existingPatient.Phone = patient.Phone;
            existingPatient.DateOfBirth = patient.DateOfBirth;
            existingPatient.Gender = patient.Gender;
            existingPatient.Address = patient.Address;
            existingPatient.City = patient.City;
            existingPatient.State = patient.State;
            existingPatient.Country = patient.Country;
            existingPatient.PostalCode = patient.PostalCode;
            existingPatient.BloodGroup = patient.BloodGroup;
            existingPatient.EmergencyContactName = patient.EmergencyContactName;
            existingPatient.EmergencyContactPhone = patient.EmergencyContactPhone;
            existingPatient.MedicalHistory = patient.MedicalHistory;
            existingPatient.Allergies = patient.Allergies;

            await _context.SaveChangesAsync();
            return existingPatient;
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return false;

            patient.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Patient>> SearchPatientsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllPatientsAsync();

            return await _context.Patients
                .Where(p => p.IsActive && (
                    p.FirstName.Contains(searchTerm) ||
                    p.LastName.Contains(searchTerm) ||
                    p.Email.Contains(searchTerm) ||
                    p.Phone.Contains(searchTerm) ||
                    p.PatientId.Contains(searchTerm)))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        private string GeneratePatientId()
        {
            // Generate unique patient ID: PTN + YYYY + sequential number
            var year = DateTime.UtcNow.Year;
            var lastPatient = _context.Patients
                .Where(p => p.PatientId.StartsWith($"PTN{year}"))
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();

            int sequentialNumber = 1;
            if (lastPatient != null)
            {
                var lastNumber = lastPatient.PatientId.Substring(7); // Remove "PTNYYYY"
                if (int.TryParse(lastNumber, out int num))
                    sequentialNumber = num + 1;
            }

            return $"PTN{year}{sequentialNumber:D6}";
        }
    }
}