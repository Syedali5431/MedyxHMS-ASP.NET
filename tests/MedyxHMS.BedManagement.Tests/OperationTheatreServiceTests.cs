using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

public class OperationTheatreServiceTests
{
    private static readonly DateTime Day = DateTime.Today.AddDays(3);

    [Fact]
    public async Task FindConflicts_RefusesOverlapAndTurnover_InSameTheatre()
    {
        await using var context = await SeedAsync();
        var service = new OperationTheatreService(context);

        // Existing case 09:00-10:00 in OT-1 with 30 min turnover: 10:15 is too early, 10:30 is fine.
        var tooEarly = await service.FindConflictsAsync(Booking(theatreId: 1, start: Day.AddHours(10).AddMinutes(15), surgeonId: 2, patientId: 2));
        var fine = await service.FindConflictsAsync(Booking(theatreId: 1, start: Day.AddHours(10).AddMinutes(30), surgeonId: 2, patientId: 2));

        Assert.Contains(tooEarly, p => p.Contains("OT-1 is booked"));
        Assert.Empty(fine);
    }

    [Fact]
    public async Task FindConflicts_RefusesSurgeonAndPatientDoubleBooking_InAnotherTheatre()
    {
        await using var context = await SeedAsync();
        var service = new OperationTheatreService(context);

        var sameSurgeon = await service.FindConflictsAsync(Booking(theatreId: 2, start: Day.AddHours(9).AddMinutes(30), surgeonId: 1, patientId: 2));
        var samePatient = await service.FindConflictsAsync(Booking(theatreId: 2, start: Day.AddHours(9).AddMinutes(30), surgeonId: 2, patientId: 1));

        Assert.Contains(sameSurgeon, p => p.Contains("surgeon is already booked"));
        Assert.Contains(samePatient, p => p.Contains("patient already has an operation"));
    }

    [Fact]
    public async Task FindConflicts_ChecksOpeningHoursAndBlocks_UnlessEmergencyOr24Hours()
    {
        await using var context = await SeedAsync();
        context.OTBlocks.Add(new OTBlock { OperationTheatreId = 2, StartsAt = Day.AddHours(13), EndsAt = Day.AddHours(15), Reason = "Maintenance" });
        await context.SaveChangesAsync();
        var service = new OperationTheatreService(context);

        var lateEvening = await service.FindConflictsAsync(Booking(theatreId: 2, start: Day.AddHours(19).AddMinutes(30), surgeonId: 2, patientId: 2));
        var lateEmergency = await service.FindConflictsAsync(Booking(theatreId: 2, start: Day.AddHours(19).AddMinutes(30), surgeonId: 2, patientId: 2, emergency: true));
        var inBlock = await service.FindConflictsAsync(Booking(theatreId: 2, start: Day.AddHours(14), surgeonId: 2, patientId: 2));
        var nightIn24h = await service.FindConflictsAsync(Booking(theatreId: 3, start: Day.AddHours(23), surgeonId: 2, patientId: 2, minutes: 30));

        Assert.Contains(lateEvening, p => p.Contains("is open 08:00–20:00"));
        Assert.Empty(lateEmergency);
        Assert.Contains(inBlock, p => p.Contains("blocked") && p.Contains("Maintenance"));
        Assert.Empty(nightIn24h);
    }

    [Fact]
    public async Task FindConflicts_IgnoresCancelledCasesAndTheBookingItself()
    {
        await using var context = await SeedAsync();
        var existing = await context.OTSchedules.FirstAsync();
        var service = new OperationTheatreService(context);

        // Moving the existing case by 15 minutes does not clash with itself.
        var moved = Booking(theatreId: 1, start: existing.ScheduledDate.AddMinutes(15), surgeonId: 1, patientId: 1);
        Assert.Empty(await service.FindConflictsAsync(moved, existing.Id));

        existing.Status = OperationTheatreService.Cancelled;
        await context.SaveChangesAsync();
        Assert.Empty(await service.FindConflictsAsync(Booking(theatreId: 1, start: existing.ScheduledDate, surgeonId: 2, patientId: 2)));
    }

    [Fact]
    public void FreeWindows_LeaveTurnoverBeforeNextCase_AndRespectBlocks()
    {
        var theatre = new OperationTheatre { Id = 1, Code = "OT-1", OpensAt = new TimeSpan(8, 0, 0), ClosesAt = new TimeSpan(20, 0, 0), TurnoverMinutes = 30 };
        var cases = new[]
        {
            new OTSchedule { ScheduledDate = Day.AddHours(10), EstimatedDurationMinutes = 120, Status = OperationTheatreService.Scheduled },
            new OTSchedule { ScheduledDate = Day.AddHours(16), EstimatedDurationMinutes = 60, Status = OperationTheatreService.Cancelled },
        };
        var blocks = new[] { new OTBlock { StartsAt = Day.AddHours(15), EndsAt = Day.AddHours(17) } };

        var free = OperationTheatreService.FreeWindows(theatre, Day.AddHours(8), Day.AddHours(20), cases, blocks, Day.AddDays(-1));

        // 08:00-10:00 (case of up to 90 min, turnover before the 10:00 case), 12:30-15:00 (block needs no turnover), 17:00-20:00.
        Assert.Equal(3, free.Count);
        Assert.Equal((Day.AddHours(8), Day.AddHours(10), 90), (free[0].From, free[0].To, free[0].LongestCaseMinutes));
        Assert.Equal((Day.AddHours(12).AddMinutes(30), Day.AddHours(15), 150), (free[1].From, free[1].To, free[1].LongestCaseMinutes));
        Assert.Equal((Day.AddHours(17), Day.AddHours(20), 180), (free[2].From, free[2].To, free[2].LongestCaseMinutes));
    }

    [Fact]
    public async Task ChangeStatus_EnforcesWorkflow_AndWithdrawsUnpaidChargeOnCancel()
    {
        await using var context = await SeedAsync();
        var service = new OperationTheatreService(context);
        var created = await service.CreateScheduleAsync(Booking(theatreId: 2, start: Day.AddHours(11), surgeonId: 2, patientId: 2, minutes: 90));
        var bill = await context.Bills.Include(b => b.BillItems).FirstAsync(b => b.Id == created.BillId);
        Assert.Equal(OperationTheatreService.CalculateCharge(90), bill.TotalAmount);
        Assert.Equal("OT-2", created.OperationTheatreNumber);
        Assert.Equal("Dr. Second Surgeon", created.SurgeonName);

        var startFuture = await service.ChangeStatusAsync(created.Id, OperationTheatreService.InProgress, null);
        var completeFuture = await service.ChangeStatusAsync(created.Id, OperationTheatreService.Completed, null);
        var cancelNoReason = await service.ChangeStatusAsync(created.Id, OperationTheatreService.Cancelled, " ");
        var cancel = await service.ChangeStatusAsync(created.Id, OperationTheatreService.Cancelled, "Patient unfit");
        var reopen = await service.ChangeStatusAsync(created.Id, OperationTheatreService.Scheduled, null);

        Assert.False(startFuture.Ok);
        Assert.False(completeFuture.Ok);
        Assert.False(cancelNoReason.Ok);
        Assert.True(cancel.Ok);
        Assert.False(reopen.Ok);
        bill = await context.Bills.Include(b => b.BillItems).FirstAsync(b => b.Id == created.BillId);
        Assert.Equal(0m, bill.TotalAmount);
        Assert.Equal(0m, bill.PendingAmount);
        Assert.All(bill.BillItems, i => Assert.Equal(0m, i.TotalPrice));
    }

    private static OTSchedule Booking(int theatreId, DateTime start, int surgeonId, int patientId, int minutes = 60, bool emergency = false) => new()
    {
        OperationTheatreId = theatreId,
        ScheduledDate = start,
        EstimatedDurationMinutes = minutes,
        SurgeonDoctorId = surgeonId,
        PatientId = patientId,
        IsEmergency = emergency,
        ProcedureName = "Test procedure",
        SurgeonName = string.Empty,
        OperationTheatreNumber = string.Empty,
        Notes = string.Empty,
        Status = OperationTheatreService.Scheduled
    };

    private static async Task<ApplicationDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);
        context.OperationTheatres.AddRange(
            new OperationTheatre { Id = 1, Code = "OT-1", Name = "Theatre 1" },
            new OperationTheatre { Id = 2, Code = "OT-2", Name = "Theatre 2" },
            new OperationTheatre { Id = 3, Code = "OT-3", Name = "Emergency", Is24Hours = true });
        context.Doctors.AddRange(
            new Doctor { Id = 1, FirstName = "First", LastName = "Surgeon" },
            new Doctor { Id = 2, FirstName = "Second", LastName = "Surgeon" });
        context.Patients.AddRange(
            new Patient { Id = 1, FirstName = "Pat", LastName = "One" },
            new Patient { Id = 2, FirstName = "Pat", LastName = "Two" });
        context.OTSchedules.Add(Booking(theatreId: 1, start: Day.AddHours(9), surgeonId: 1, patientId: 1));
        await context.SaveChangesAsync();
        return context;
    }
}
