using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class PrescriptionServiceTests
{
    [Fact]
    public async Task CreatePrescriptionAsync_ShouldCalculateTotalPrice()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        var patient = ModelFactory.CreatePatient();
        var medicine = ModelFactory.CreateMedicine();
        context.Patients.Add(patient);
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();

        var bill = new PharmacyBill
        {
            BillNumber = "PHARM-001",
            PatientId = patient.Id,
            BillDate = DateTime.UtcNow.Date,
            TotalAmount = 0m,
            PaidAmount = 0m,
            Status = "Pending",
            PaymentMethod = "Cash",
            Notes = "Test bill",
            CreatedBy = "test",
            Prescriptions = new List<Prescription>()
        };
        context.PharmacyBills.Add(bill);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var prescription = new Prescription
        {
            PharmacyBillId = bill.Id,
            MedicineId = medicine.Id,
            Dosage = "1 tablet",
            Frequency = "Twice daily",
            Duration = 5,
            Quantity = 6,
            UnitPrice = 5m,
            Instructions = "After food"
        };

        var created = await service.CreatePrescriptionAsync(prescription);

        Assert.Equal(30m, created.TotalPrice);
        Assert.True(created.CreatedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateMedicineStockAsync_ShouldDecreaseStockWhenEnoughInventoryExists()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        var medicine = ModelFactory.CreateMedicine();
        context.Medicines.Add(medicine);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var updated = await service.UpdateMedicineStockAsync(medicine.Id, 15);
        var stored = await context.Medicines.FindAsync(medicine.Id);

        Assert.True(updated);
        Assert.NotNull(stored);
        Assert.Equal(85, stored!.StockQuantity);
    }

    [Fact]
    public async Task GetLowStockMedicinesAsync_ShouldReturnOnlyActiveLowStockEntries()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        var lowStock = ModelFactory.CreateMedicine(1, "LowStock");
        lowStock.StockQuantity = 5;
        lowStock.MinStockLevel = 10;

        var healthyStock = ModelFactory.CreateMedicine(2, "Healthy");
        healthyStock.StockQuantity = 40;

        var inactiveLowStock = ModelFactory.CreateMedicine(3, "InactiveLowStock");
        inactiveLowStock.StockQuantity = 2;
        inactiveLowStock.MinStockLevel = 10;
        inactiveLowStock.IsActive = false;

        context.Medicines.AddRange(lowStock, healthyStock, inactiveLowStock);
        await context.SaveChangesAsync();

        var service = new PrescriptionService(context);
        var results = (await service.GetLowStockMedicinesAsync()).ToList();

        Assert.Single(results);
        Assert.Equal("LowStock", results[0].Name);
    }
}