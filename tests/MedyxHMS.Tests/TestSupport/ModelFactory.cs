using MedyxHMS.Models;

namespace MedyxHMS.Tests.TestSupport;

internal static class ModelFactory
{
    public static Patient CreatePatient(string firstName = "John", string lastName = "Doe")
    {
        return new Patient
        {
            PatientId = $"TEST{Guid.NewGuid():N}"[..14],
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
            Phone = "1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            Address = "Address",
            City = "City",
            State = "State",
            Country = "Country",
            PostalCode = "1000",
            BloodGroup = "A+",
            EmergencyContactName = "Jane Doe",
            EmergencyContactPhone = "1112223333",
            EmergencyContactRelation = "Spouse",
            MedicalHistory = "None",
            Allergies = "None",
            GuardianName = "Guardian",
            GuardianPhone = "9998887777",
            MaritalStatus = "Married",
            Occupation = "Engineer",
            UserId = string.Empty,
            ProfileImagePath = string.Empty,
            IsActive = true
        };
    }

    public static Doctor CreateDoctor(int id = 1)
    {
        return new Doctor
        {
            Id = id,
            EmployeeId = $"DOC{id:000}",
            FirstName = "Amy",
            LastName = "Smith",
            Specialization = "General",
            LicenseNumber = $"LIC{id:000}",
            Phone = "2223334444",
            Email = $"doctor{id}@example.com",
            DepartmentId = 1,
            IsActive = true
        };
    }

    public static Department CreateDepartment(int id = 1)
    {
        return new Department
        {
            Id = id,
            Name = "General Medicine",
            Description = "General",
            HeadOfDepartment = "Dr Head",
            IsActive = true
        };
    }
}
