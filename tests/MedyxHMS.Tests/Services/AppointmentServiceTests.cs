using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class AppointmentServiceTests
{
    [Fact]
    public async Task CreateAppointmentAsync_ShouldSetScheduledStatusAndCreatedDate()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = 1,
            AppointmentDate = DateTime.UtcNow.Date.AddDays(1),
            AppointmentTime = new TimeSpan(10, 0, 0),
            AppointmentType = "OPD",
            Priority = "Normal",
            Symptoms = "Headache",
            Notes = "N/A",
            CreatedBy = "test",
            UpdatedBy = "test"
        };

        var created = await service.CreateAppointmentAsync(appointment);

        Assert.Equal("Scheduled", created.Status);
        Assert.True(created.CreatedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetAppointmentsByDateAsync_ShouldReturnOnlyAppointmentsForSelectedDateInTimeOrder()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var targetDate = DateTime.UtcNow.Date.AddDays(2);
        context.Appointments.AddRange(
            new Appointment { PatientId = patient.Id, DoctorId = 1, AppointmentDate = targetDate, AppointmentTime = new TimeSpan(11, 0, 0), Status = "Scheduled", AppointmentType = "OPD", Priority = "Normal", Symptoms = "A", Notes = "A", CreatedBy = "test", UpdatedBy = "test" },
            new Appointment { PatientId = patient.Id, DoctorId = 1, AppointmentDate = targetDate, AppointmentTime = new TimeSpan(9, 0, 0), Status = "Scheduled", AppointmentType = "OPD", Priority = "Normal", Symptoms = "B", Notes = "B", CreatedBy = "test", UpdatedBy = "test" },
            new Appointment { PatientId = patient.Id, DoctorId = 1, AppointmentDate = targetDate.AddDays(1), AppointmentTime = new TimeSpan(8, 0, 0), Status = "Scheduled", AppointmentType = "OPD", Priority = "Normal", Symptoms = "C", Notes = "C", CreatedBy = "test", UpdatedBy = "test" }
        );
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);
        var result = (await service.GetAppointmentsByDateAsync(targetDate)).ToList();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].AppointmentTime <= result[1].AppointmentTime);
        Assert.All(result, a => Assert.Equal(targetDate.Date, a.AppointmentDate.Date));
    }

    [Fact]
    public async Task DeleteAppointmentAsync_ShouldRemoveAppointment()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = 1,
            AppointmentDate = DateTime.UtcNow.Date,
            AppointmentTime = new TimeSpan(10, 30, 0),
            Status = "Scheduled",
            AppointmentType = "OPD",
            Priority = "Normal",
            Symptoms = "X",
            Notes = "Y",
            CreatedBy = "test",
            UpdatedBy = "test"
        };
        context.Appointments.Add(appointment);
        await context.SaveChangesAsync();

        var service = new AppointmentService(context);
        var deleted = await service.DeleteAppointmentAsync(appointment.Id);
        var exists = await context.Appointments.FindAsync(appointment.Id);

        Assert.True(deleted);
        Assert.Null(exists);
    }
}
