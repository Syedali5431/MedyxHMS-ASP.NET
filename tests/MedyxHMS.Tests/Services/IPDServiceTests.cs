using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class IPDServiceTests
{
    [Fact]
    public async Task CreateIPDAdmissionAsync_ShouldOccupyAssignedBedAndSetAdmittedStatus()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        context.Patients.Add(ModelFactory.CreatePatient());
        context.Wards.Add(ModelFactory.CreateWard());
        context.Beds.Add(ModelFactory.CreateBed());
        await context.SaveChangesAsync();

        var service = new IPDService(context);
        var admission = new IPDAdmission
        {
            PatientId = 1,
            DoctorId = 1,
            BedId = 1,
            AdmissionDate = DateTime.UtcNow.Date,
            AdmissionType = "Planned",
            Diagnosis = "Observation",
            Treatment = "Rest",
            Notes = "N/A",
            DailyCharges = 125m,
            CreatedBy = "test"
        };

        var created = await service.CreateIPDAdmissionAsync(admission);
        var bed = await context.Beds.FindAsync(1);

        Assert.Equal("Admitted", created.Status);
        Assert.NotNull(bed);
        Assert.Equal("Occupied", bed!.Status);
    }

    [Fact]
    public async Task DischargePatientAsync_ShouldCreateBillingRecordAndReleaseBed()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        context.Patients.Add(ModelFactory.CreatePatient());
        context.Wards.Add(ModelFactory.CreateWard());
        context.Beds.Add(ModelFactory.CreateBed());
        await context.SaveChangesAsync();

        var admission = new IPDAdmission
        {
            PatientId = 1,
            DoctorId = 1,
            BedId = 1,
            AdmissionDate = DateTime.UtcNow.Date.AddDays(-3),
            AdmissionType = "Emergency",
            Diagnosis = "Recovery",
            Treatment = "Observation",
            Notes = "Admitted",
            Status = "Admitted",
            DailyCharges = 200m,
            CreatedBy = "test"
        };
        context.IPDAdmissions.Add(admission);
        var occupiedBed = await context.Beds.FindAsync(1);
        occupiedBed!.Status = "Occupied";
        await context.SaveChangesAsync();

        var service = new IPDService(context);
        var dischargeDate = DateTime.UtcNow.Date;

        var discharged = await service.DischargePatientAsync(admission.Id, dischargeDate);
        var updatedAdmission = await context.IPDAdmissions.FindAsync(admission.Id);
        var updatedBed = await context.Beds.FindAsync(1);
        var bill = context.Bills.SingleOrDefault();

        Assert.True(discharged);
        Assert.NotNull(updatedAdmission);
        Assert.Equal("Discharged", updatedAdmission!.Status);
        Assert.Equal(dischargeDate, updatedAdmission.DischargeDate);
        Assert.NotNull(updatedBed);
        Assert.Equal("Available", updatedBed!.Status);
        Assert.NotNull(bill);
        Assert.Equal("IPD", bill!.BillType);
        Assert.Equal(600m, bill.TotalAmount);
        Assert.Single(bill.BillItems);
        Assert.Equal(3, bill.BillItems.First().Quantity);
    }
}