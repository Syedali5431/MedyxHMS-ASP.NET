using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class BillingServiceTests
{
    [Fact]
    public async Task CreateBillAsync_ShouldAssignBillNumberAndPendingAmount()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var service = new BillingService(context);

        var bill = new Bill
        {
            PatientId = patient.Id,
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            TotalAmount = 150m,
            BillType = "OPD",
            Notes = "Consultation",
            CreatedBy = "test"
        };

        var created = await service.CreateBillAsync(bill);

        Assert.StartsWith($"BILL{DateTime.UtcNow:yyyyMMdd}", created.BillNumber);
        Assert.Equal(150m, created.PendingAmount);
        Assert.Equal("Unpaid", created.Status);
    }

    [Fact]
    public async Task UpdateBillAsync_ShouldRecalculatePendingAndSetPaidStatus()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var bill = new Bill
        {
            BillNumber = "BILLTEST0001",
            PatientId = patient.Id,
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(3),
            TotalAmount = 200m,
            PaidAmount = 0m,
            PendingAmount = 200m,
            Status = "Unpaid",
            BillType = "OPD",
            Notes = "Initial",
            CreatedBy = "test"
        };
        context.Bills.Add(bill);
        await context.SaveChangesAsync();

        var service = new BillingService(context);
        bill.PaidAmount = 200m;
        bill.TotalAmount = 200m;
        bill.Notes = "Paid in full";

        var updated = await service.UpdateBillAsync(bill);

        Assert.NotNull(updated);
        Assert.Equal(0m, updated!.PendingAmount);
        Assert.Equal("Paid", updated.Status);
    }

    [Fact]
    public async Task GetTotalRevenueAsync_ShouldSumOnlyCompletedPaymentsWithinRange()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var start = DateTime.UtcNow.Date.AddDays(-7);
        var end = DateTime.UtcNow.Date.AddDays(1);

        context.Payments.AddRange(
            new Payment { BillId = 1, Amount = 100m, PaymentDate = DateTime.UtcNow.Date, Status = "Completed", PaymentMethod = "Cash", TransactionId = "T1", PaymentGateway = "Cash", Notes = "", ProcessedBy = "system" },
            new Payment { BillId = 2, Amount = 40m, PaymentDate = DateTime.UtcNow.Date, Status = "Pending", PaymentMethod = "Cash", TransactionId = "T2", PaymentGateway = "Cash", Notes = "", ProcessedBy = "system" },
            new Payment { BillId = 3, Amount = 60m, PaymentDate = DateTime.UtcNow.Date.AddDays(-20), Status = "Completed", PaymentMethod = "Cash", TransactionId = "T3", PaymentGateway = "Cash", Notes = "", ProcessedBy = "system" }
        );
        await context.SaveChangesAsync();

        var service = new BillingService(context);
        var total = await service.GetTotalRevenueAsync(start, end);

        Assert.Equal(100m, total);
    }
}
