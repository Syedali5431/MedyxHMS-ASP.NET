using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

// Multi-hospital: records of the newly grouped modules belong to the hospital they are recorded in.
public class HospitalGroupTests
{
    private static DbContextOptions<ApplicationDbContext> NewDatabase() =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    // Active hospital: filter on, new records saved there. null = all hospitals (filter off, new records to defaultId).
    private static ApplicationDbContext InHospital(DbContextOptions<ApplicationDbContext> options, int? hospitalId, int defaultId = 1) =>
        new(options, new HospitalContext
        {
            FilterEnabled = hospitalId.HasValue,
            ActiveHospitalId = hospitalId,
            HospitalIdForNewRecords = hospitalId ?? defaultId,
            DefaultHospitalId = defaultId,
            IsStaffContext = true
        });

    private static async Task SeedHospitalsAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        db.Hospitals.AddRange(
            new Hospital { Id = 1, Code = "MAIN", Name = "Main Hospital", IsActive = true, IsDefault = true },
            new Hospital { Id = 2, Code = "ISB", Name = "City General Hospital", IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task NewRecords_BelongToTheActiveHospital_AndAreHiddenFromOthers()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);

        await using (var isb = InHospital(options, 2))
        {
            isb.LabResults.Add(new LabResult { PatientId = 1, LabTestId = 1, OrderNumber = "LAB-1", Status = "Ordered", ResultValue = "", NormalRange = "", Unit = "", Interpretation = "", PerformedBy = "", VerifiedBy = "", Notes = "" });
            isb.VisitorLogs.Add(new VisitorLog { VisitorName = "Visitor", Phone = "", Purpose = "", PersonToMeet = "", Status = "In", Notes = "" });
            isb.BirthRecords.Add(new BirthRecord { BabyName = "Baby", Gender = "Female", MotherName = "Mother", FatherName = "", GuardianContact = "", DeliveryType = "Normal", AttendingDoctorName = "", CertificateNumber = "BR-1", Notes = "" });
            await isb.SaveChangesAsync();
        }

        await using (var raw = new ApplicationDbContext(options))
        {
            Assert.All(await raw.LabResults.ToListAsync(), r => Assert.Equal(2, r.HospitalId));
            Assert.All(await raw.VisitorLogs.ToListAsync(), v => Assert.Equal(2, v.HospitalId));
            Assert.All(await raw.BirthRecords.ToListAsync(), b => Assert.Equal(2, b.HospitalId));
        }

        await using (var main = InHospital(options, 1))
        {
            Assert.Empty(await main.LabResults.ToListAsync());
            Assert.Empty(await main.VisitorLogs.ToListAsync());
            Assert.Empty(await main.BirthRecords.ToListAsync());
        }

        await using (var all = InHospital(options, null))
        {
            Assert.Single(await all.LabResults.ToListAsync());
        }
    }

    [Fact]
    public async Task AmbulanceTrip_TakesTheHospitalOfItsVehicle()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using (var isb = InHospital(options, 2))
        {
            isb.AmbulanceVehicles.Add(new AmbulanceVehicle { Id = 7, VehicleNumber = "ISB-1", DriverName = "", DriverContact = "", Model = "", Status = "Available", Notes = "" });
            await isb.SaveChangesAsync();
        }

        // Recorded while all hospitals are shown (new records would go to the default hospital 1).
        await using (var all = InHospital(options, null))
        {
            all.AmbulanceDispatches.Add(new AmbulanceDispatch { AmbulanceVehicleId = 7, PatientName = "P", PickupAddress = "", ContactNumber = "", Purpose = "", Status = "Dispatched", Notes = "" });
            await all.SaveChangesAsync();
        }

        await using var raw = new ApplicationDbContext(options);
        Assert.Equal(2, (await raw.AmbulanceDispatches.SingleAsync()).HospitalId);
    }

    [Fact]
    public async Task TpaClaim_TakesTheHospitalOfItsBill()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using (var isb = InHospital(options, 2))
        {
            isb.Bills.Add(new Bill { Id = 50, BillNumber = "B-50", PatientId = 1, Status = "Unpaid", BillType = "OPD", Notes = "" });
            await isb.SaveChangesAsync();
        }

        await using (var all = InHospital(options, null))
        {
            all.TpaClaims.Add(new TpaClaim { TpaProviderId = 1, PatientId = 1, BillId = 50, ClaimNumber = "TPA-1", Status = "Pending", Remarks = "" });
            await all.SaveChangesAsync();
        }

        await using var raw = new ApplicationDbContext(options);
        Assert.Equal(2, (await raw.TpaClaims.SingleAsync()).HospitalId);
    }

    [Fact]
    public async Task EveryHospital_GetsItsOwnBloodStock_Once()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using var db = new ApplicationDbContext(options);

        Assert.Equal(16, await HospitalSetup.EnsureBloodStockAsync(db));
        Assert.Equal(0, await HospitalSetup.EnsureBloodStockAsync(db));
        Assert.Equal(8, await db.BloodInventories.CountAsync(b => b.HospitalId == 2));
    }

    [Fact]
    public async Task BloodIssue_TakesUnitsFromTheActiveHospitalsStock()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using (var raw = new ApplicationDbContext(options))
        {
            await HospitalSetup.EnsureBloodStockAsync(raw);
            foreach (var row in await raw.BloodInventories.Where(b => b.BloodGroup == "A+").ToListAsync())
                row.UnitsAvailable = 10;
            await raw.SaveChangesAsync();
        }

        await using (var isb = InHospital(options, 2))
        {
            var issue = await new BloodBankService(isb).IssueBloodAsync(new BloodIssue { PatientId = 1, BloodGroup = "A+", UnitsIssued = 3, RequestedBy = "", CrossMatchStatus = "Compatible", Notes = "" });
            Assert.Equal(2, issue.HospitalId);
        }

        await using var check = new ApplicationDbContext(options);
        Assert.Equal(7, (await check.BloodInventories.SingleAsync(b => b.HospitalId == 2 && b.BloodGroup == "A+")).UnitsAvailable);
        Assert.Equal(10, (await check.BloodInventories.SingleAsync(b => b.HospitalId == 1 && b.BloodGroup == "A+")).UnitsAvailable);
        Assert.Equal(2, (await check.Bills.SingleAsync()).HospitalId);
    }

    private static Medicine NewMedicine(int hospitalId, string name, int stock) => new()
    {
        HospitalId = hospitalId, Name = name, GenericName = name, Category = "Analgesic", DosageForm = "Tablet", Strength = "500mg",
        Manufacturer = "", UnitPrice = 5m, StockQuantity = stock, MinStockLevel = 10, ExpiryDate = DateTime.Today.AddYears(1), BatchNumber = "B1", IsActive = true
    };

    private static PharmacyBill NewPharmacyBill(int id, int hospitalId) => new()
    {
        Id = id, HospitalId = hospitalId, PatientId = 1, BillNumber = $"RX-{id}", BillDate = DateTime.Now, Status = "Pending", PaymentMethod = "", Notes = "", CreatedBy = ""
    };

    private static Prescription NewPrescription(int billId, int medicineId, int quantity) => new()
    {
        PharmacyBillId = billId, MedicineId = medicineId, Dosage = "1 tablet", Frequency = "Daily", Duration = 5, Quantity = quantity, UnitPrice = 5m, Instructions = ""
    };

    [Fact]
    public async Task EveryHospital_HasItsOwnMedicines_AndPrescriptionsUseThem()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using (var raw = new ApplicationDbContext(options))
        {
            raw.Medicines.Add(NewMedicine(1, "Paracetamol", 100));
            raw.PharmacyBills.Add(NewPharmacyBill(20, 2));
            await raw.SaveChangesAsync();
            // An Islamabad prescription that still points at the Main Hospital medicine (data from before the change).
            raw.Prescriptions.Add(NewPrescription(20, (await raw.Medicines.SingleAsync()).Id, 4));
            await raw.SaveChangesAsync();

            Assert.True(await HospitalSetup.EnsureMedicinesAsync(raw) > 0);
            Assert.Equal(0, await HospitalSetup.EnsureMedicinesAsync(raw));
        }

        await using var check = new ApplicationDbContext(options);
        var isbMedicine = await check.Medicines.SingleAsync(m => m.HospitalId == 2);
        Assert.Equal("Paracetamol", isbMedicine.Name);
        Assert.Equal(0, isbMedicine.StockQuantity);                       // another hospital's stock is not copied
        Assert.Equal(100, (await check.Medicines.SingleAsync(m => m.HospitalId == 1)).StockQuantity);
        Assert.Equal(isbMedicine.Id, (await check.Prescriptions.SingleAsync()).MedicineId);

        await using var main = InHospital(options, 1);
        Assert.Single(await main.Medicines.ToListAsync());                 // each hospital lists only its own medicines
    }

    [Fact]
    public async Task Dispensing_TakesFromStock_RefusesTooMuch_AndDeletingReturnsIt()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        int medicineId;
        await using (var raw = new ApplicationDbContext(options))
        {
            raw.Medicines.Add(NewMedicine(2, "Amoxicillin", 10));
            raw.Medicines.Add(NewMedicine(1, "Amoxicillin", 50));
            raw.PharmacyBills.Add(NewPharmacyBill(30, 2));
            await raw.SaveChangesAsync();
            medicineId = (await raw.Medicines.SingleAsync(m => m.HospitalId == 2)).Id;
        }

        int prescriptionId;
        await using (var isb = InHospital(options, 2))
        {
            var service = new PrescriptionService(isb);
            prescriptionId = (await service.CreatePrescriptionAsync(NewPrescription(30, medicineId, 6))).Id;
            var tooMuch = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePrescriptionAsync(NewPrescription(30, medicineId, 5)));
            Assert.Contains("Not enough", tooMuch.Message);
            var mainMedicine = await isb.Medicines.IgnoreQueryFilters().SingleAsync(m => m.HospitalId == 1);
            var otherHospital = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePrescriptionAsync(NewPrescription(30, mainMedicine.Id, 1)));
            Assert.Contains("another hospital", otherHospital.Message);
        }

        await using (var check = new ApplicationDbContext(options))
            Assert.Equal(4, (await check.Medicines.SingleAsync(m => m.Id == medicineId)).StockQuantity);

        await using (var isb = InHospital(options, 2))
            Assert.True(await new PrescriptionService(isb).DeletePrescriptionAsync(prescriptionId));

        await using (var check = new ApplicationDbContext(options))
        {
            Assert.Equal(10, (await check.Medicines.SingleAsync(m => m.Id == medicineId)).StockQuantity);
            Assert.Equal(50, (await check.Medicines.SingleAsync(m => m.HospitalId == 1)).StockQuantity);
        }
    }

    [Fact]
    public async Task NewHospital_GetsTheMedicineListWithoutStock()
    {
        var options = NewDatabase();
        await SeedHospitalsAsync(options);
        await using var db = new ApplicationDbContext(options);
        db.Medicines.Add(NewMedicine(1, "Cetirizine", 70));
        db.Medicines.Add(NewMedicine(1, "Omeprazole", 40));
        await db.SaveChangesAsync();

        Assert.Equal(2, await HospitalSetup.EnsureMedicinesAsync(db, 2));
        Assert.All(await db.Medicines.Where(m => m.HospitalId == 2).ToListAsync(), m => Assert.Equal(0, m.StockQuantity));
    }
}
