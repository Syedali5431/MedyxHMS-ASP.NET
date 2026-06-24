// Purpose: Contains application code for Patient and its related runtime behavior.
namespace MedyxHMS.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string PatientId { get; set; } = string.Empty; // Unique patient identifier
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female, Other
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string BloodGroup { get; set; } = string.Empty;
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyContactRelation { get; set; } = string.Empty;
        public string MedicalHistory { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string ProfileImagePath { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool HasInsurance { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastVisitDate { get; set; }

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<OPDVisit> OPDVisits { get; set; } = new List<OPDVisit>();
        public ICollection<IPDAdmission> IPDAdmissions { get; set; } = new List<IPDAdmission>();
        public ICollection<Bill> Bills { get; set; } = new List<Bill>();
        public ICollection<PatientInsurance> Insurances { get; set; } = new List<PatientInsurance>();
        public ICollection<SystemNotification> Notifications { get; set; } = new List<SystemNotification>();
        public ApplicationUser? User { get; set; }
    }

    public class Appointment
    {
        public int Id { get; set; }
        public int AppointmentId
        {
            get => Id;
            set => Id = value;
        }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string StaffId
        {
            get => DoctorId.ToString();
            set
            {
                if (int.TryParse(value, out var doctorId))
                {
                    DoctorId = doctorId;
                }
            }
        }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } = string.Empty; // Scheduled, Confirmed, Completed, Cancelled
        public string AppointmentType { get; set; } = string.Empty; // OPD, Consultation, Follow-up
        public string Priority { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public bool CanReschedule => AppointmentDate.Date > DateTime.Today && Status != "Completed" && Status != "Cancelled";

        // Navigation properties
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public Staff Staff { get; set; } = null!;
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string Name => $"{FirstName} {LastName}".Trim();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Department Department { get; set; } = null!;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HeadOfDepartment { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    }
}
