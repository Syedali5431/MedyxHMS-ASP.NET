using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

public class BedServiceTests
{
    [Fact]
    public async Task AssignBedAsync_Fails_WhenBedIsNotAvailable()
    {
        await using var context = CreateContext();
        context.Wards.Add(new Ward { Id = 1, Name = "Ward A", IsActive = true });
        context.Beds.Add(new Bed
        {
            Id = 10,
            WardId = 1,
            BedNumber = "A-10",
            BedType = "General",
            Status = "Cleaning",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new BedService(context);

        var result = await service.AssignBedAsync(10, 200, "Nurse");

        Assert.False(result.Success);
        Assert.Contains("not available", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignBedAsync_Fails_ForIcuBed_WhenRequesterLacksApprovalRole()
    {
        await using var context = CreateContext();
        context.Wards.Add(new Ward { Id = 1, Name = "ICU", IsActive = true });
        context.Beds.Add(new Bed
        {
            Id = 11,
            WardId = 1,
            BedNumber = "ICU-01",
            BedType = "ICU",
            Status = "Available",
            IsActive = true,
            RequiresAdminApproval = true
        });
        await context.SaveChangesAsync();

        var service = new BedService(context);

        var result = await service.AssignBedAsync(11, 201, "Doctor");

        Assert.False(result.Success);
        Assert.Contains("Admin approval", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseBedAsync_ClearsPatientAndSetsCleaning()
    {
        await using var context = CreateContext();
        context.Wards.Add(new Ward { Id = 1, Name = "Ward B", IsActive = true });
        context.Beds.Add(new Bed
        {
            Id = 12,
            WardId = 1,
            BedNumber = "B-12",
            BedType = "General",
            Status = "Occupied",
            PatientId = 300,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new BedService(context);

        var result = await service.ReleaseBedAsync(12);
        var bed = await context.Beds.SingleAsync(b => b.Id == 12);

        Assert.True(result.Success);
        Assert.Null(bed.PatientId);
        Assert.Equal("Cleaning", bed.Status);
        Assert.NotNull(bed.LastUpdated);
    }

    [Fact]
    public async Task TransferBedAsync_MovesPatient_AndSetsSourceToCleaning()
    {
        await using var context = CreateContext();
        context.Wards.Add(new Ward { Id = 1, Name = "Ward C", IsActive = true });
        context.Beds.AddRange(
            new Bed
            {
                Id = 13,
                WardId = 1,
                BedNumber = "C-13",
                BedType = "General",
                Status = "Occupied",
                PatientId = 301,
                IsActive = true
            },
            new Bed
            {
                Id = 14,
                WardId = 1,
                BedNumber = "C-14",
                BedType = "General",
                Status = "Available",
                IsActive = true
            });
        await context.SaveChangesAsync();

        var service = new BedService(context);

        var result = await service.TransferBedAsync(13, 14);
        var source = await context.Beds.SingleAsync(b => b.Id == 13);
        var target = await context.Beds.SingleAsync(b => b.Id == 14);

        Assert.True(result.Success);
        Assert.Null(source.PatientId);
        Assert.Equal("Cleaning", source.Status);
        Assert.Equal(301, target.PatientId);
        Assert.Equal("Occupied", target.Status);
    }

    [Fact]
    public async Task GetBedManagementSummaryAsync_CountsMaintenanceAndBlockedTogether()
    {
        await using var context = CreateContext();
        context.Wards.Add(new Ward { Id = 1, Name = "Ward D", IsActive = true });
        context.Beds.AddRange(
            CreateBed(21, 1, "D-21", "Available"),
            CreateBed(22, 1, "D-22", "Occupied"),
            CreateBed(23, 1, "D-23", "Cleaning"),
            CreateBed(24, 1, "D-24", "Maintenance"),
            CreateBed(25, 1, "D-25", "Blocked"));
        await context.SaveChangesAsync();

        var service = new BedService(context);

        var summary = await service.GetBedManagementSummaryAsync();

        Assert.Equal(5, summary.TotalBeds);
        Assert.Equal(1, summary.AvailableBeds);
        Assert.Equal(1, summary.OccupiedBeds);
        Assert.Equal(1, summary.CleaningBeds);
        Assert.Equal(2, summary.MaintenanceBeds);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Bed CreateBed(int id, int wardId, string bedNumber, string status)
    {
        return new Bed
        {
            Id = id,
            WardId = wardId,
            BedNumber = bedNumber,
            BedType = "General",
            Status = status,
            IsActive = true
        };
    }
}