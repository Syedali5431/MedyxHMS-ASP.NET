namespace MedyxHMS.Models
{
    public class Patient
    {
        public int Id { get; set; }
        public string PatientId { get; set; } // Unique patient identifier
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } // Male, Female, Other
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string BloodGroup { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactPhone { get; set; }
        public string EmergencyContactRelation { get; set; }
        public string MedicalHistory { get; set; }
        public string Allergies { get; set; }
        public string GuardianName { get; set; }
        public string GuardianPhone { get; set; }
        public string MaritalStatus { get; set; }
        public string Occupation { get; set; }
        public string UserId { get; set; }
        public string ProfileImagePath { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastVisitDate { get; set; }

        // Navigation properties
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<OPDVisit> OPDVisits { get; set; }
        public ICollection<IPDAdmission> IPDAdmissions { get; set; }
        public ICollection<Bill> Bills { get; set; }
        public ApplicationUser User { get; set; }
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
        public string Status { get; set; } // Scheduled, Confirmed, Completed, Cancelled
        public string AppointmentType { get; set; } // OPD, Consultation, Follow-up
        public string Priority { get; set; }
        public string Symptoms { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool CanReschedule => AppointmentDate.Date > DateTime.Today && Status != "Completed" && Status != "Cancelled";

        // Navigation properties
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
        public Staff Staff { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Specialization { get; set; }
        public string LicenseNumber { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int DepartmentId { get; set; }
        public string Name => $"{FirstName} {LastName}".Trim();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Department Department { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string HeadOfDepartment { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Doctor> Doctors { get; set; }
    }
}