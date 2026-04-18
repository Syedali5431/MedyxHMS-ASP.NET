using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class ReportServiceTests
{
    [Fact]
    public async Task GenerateFinancialReportAsync_ShouldAggregatePayrollBillsAndPayments()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        context.PayrollRecords.Add(new PayrollRecord
        {
            StaffId = "STF001",
            PayrollMonth = DateTime.UtcNow.Date,
            BasicSalary = 500m,
            Allowances = 50m,
            Deductions = 25m,
            NetSalary = 525m,
            Status = "Paid",
            Notes = "Processed",
            CreatedDate = DateTime.UtcNow.Date
        });
        context.Bills.Add(new Bill
        {
            BillNumber = "BILL-001",
            PatientId = patient.Id,
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            TotalAmount = 1000m,
            PaidAmount = 0m,
            PendingAmount = 1000m,
            Status = "Unpaid",
            BillType = "OPD",
            Notes = "Test",
            CreatedBy = "test",
            CreatedDate = DateTime.UtcNow.Date
        });
        context.Payments.Add(new Payment
        {
            BillId = 1,
            Amount = 400m,
            PaymentDate = DateTime.UtcNow.Date,
            Status = "Completed",
            PaymentMethod = "Cash",
            TransactionId = "TXN-001",
            PaymentGateway = "Cash",
            Notes = string.Empty,
            ProcessedBy = "test"
        });
        await context.SaveChangesAsync();

        var service = new ReportService(context);
        var report = await service.GenerateFinancialReportAsync(DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(1));

        Assert.Equal(525m, report["TotalPayroll"]);
        Assert.Equal(1000m, report["TotalBills"]);
        Assert.Equal(400m, report["TotalPayments"]);
        Assert.Equal(475m, report["NetRevenue"]);
    }

    [Fact]
    public async Task GenerateOccupancyReportAsync_ShouldReturnTotalOccupiedAndAvailableBeds()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        context.Patients.Add(ModelFactory.CreatePatient());
        context.Wards.Add(ModelFactory.CreateWard());
        context.Beds.AddRange(ModelFactory.CreateBed(1), ModelFactory.CreateBed(2));
        context.IPDAdmissions.Add(new IPDAdmission
        {
            PatientId = 1,
            DoctorId = 1,
            BedId = 1,
            AdmissionDate = DateTime.UtcNow.Date.AddDays(-1),
            AdmissionType = "Planned",
            Diagnosis = "Recovery",
            Treatment = "Observation",
            Notes = "Occupied bed",
            Status = "Admitted",
            DailyCharges = 100m,
            CreatedBy = "test"
        });
        await context.SaveChangesAsync();

        var service = new ReportService(context);
        var report = await service.GenerateOccupancyReportAsync(DateTime.UtcNow.Date);

        Assert.Equal(2, report["TotalBeds"]);
        Assert.Equal(1, report["OccupiedBeds"]);
        Assert.Equal(1, report["AvailableBeds"]);
    }
}