using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class ChatbotKnowledgeServiceTests
{
    [Fact]
    public async Task RetrieveContextAsync_ShouldIncludeMatchingCmsSources()
    {
        using var db = TestDbContextFactory.Create(nameof(RetrieveContextAsync_ShouldIncludeMatchingCmsSources));
        db.CmsPages.Add(new CmsPage
        {
            Title = "Billing Help",
            Slug = "billing-help",
            Content = "Billing module supports invoice history and pending payments.",
            Status = "Published",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var settings = new FakeSettingService();
        settings.Values["LicenseSuperAdminContact"] = "support@hospital.test";

        var sut = new ChatbotKnowledgeService(db, settings);
        var user = BuildPrincipal("u1", "Patient");

        var context = await sut.RetrieveContextAsync(user, "How do I pay a pending billing invoice?");

        Assert.Contains(context.Sources, s => s.SourceName == "Billing Help");
        Assert.Contains(context.Sources, s => s.SourceName == "Billing Guidance");
        Assert.Contains("Grounding Sources:", context.SystemContext);
    }

    [Fact]
    public async Task RetrieveContextAsync_ShouldScopePatientSummariesToCurrentUserOnly()
    {
        using var db = TestDbContextFactory.Create(nameof(RetrieveContextAsync_ShouldScopePatientSummariesToCurrentUserOnly));

        db.Patients.AddRange(
            new Patient
            {
                Id = 1,
                UserId = "patient-1",
                PatientId = "P001",
                FirstName = "Amy",
                LastName = "One",
                Email = "amy@test.local"
            },
            new Patient
            {
                Id = 2,
                UserId = "patient-2",
                PatientId = "P002",
                FirstName = "Ben",
                LastName = "Two",
                Email = "ben@test.local"
            });

        db.Appointments.AddRange(
            new Appointment
            {
                PatientId = 1,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.Date.AddDays(2),
                AppointmentTime = TimeSpan.FromHours(9),
                Status = "Scheduled"
            },
            new Appointment
            {
                PatientId = 2,
                DoctorId = 1,
                AppointmentDate = DateTime.UtcNow.Date.AddDays(3),
                AppointmentTime = TimeSpan.FromHours(10),
                Status = "Scheduled"
            });

        db.Bills.AddRange(
            new Bill
            {
                BillNumber = "B001",
                PatientId = 1,
                BillDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(10),
                TotalAmount = 500,
                PaidAmount = 100,
                PendingAmount = 400,
                Status = "Unpaid"
            },
            new Bill
            {
                BillNumber = "B002",
                PatientId = 2,
                BillDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(10),
                TotalAmount = 900,
                PaidAmount = 0,
                PendingAmount = 900,
                Status = "Unpaid"
            });

        await db.SaveChangesAsync();

        var settings = new FakeSettingService();
        var sut = new ChatbotKnowledgeService(db, settings);
        var user = BuildPrincipal("patient-1", "Patient");

        var context = await sut.RetrieveContextAsync(user, "Show my appointment and bill summary");

        Assert.Contains("UpcomingAppointments=1", context.SystemContext);
        Assert.Contains("PendingBills=1", context.SystemContext);
        Assert.Contains("Outstanding=400", context.SystemContext);
        Assert.DoesNotContain("900", context.SystemContext);
    }

    [Fact]
    public async Task RetrieveContextAsync_ShouldReturnFallbackSourceWhenNoMatch()
    {
        using var db = TestDbContextFactory.Create(nameof(RetrieveContextAsync_ShouldReturnFallbackSourceWhenNoMatch));
        var settings = new FakeSettingService();
        var sut = new ChatbotKnowledgeService(db, settings);
        var user = BuildPrincipal("u2", "Admin");

        var context = await sut.RetrieveContextAsync(user, "qzxv plmn random input");

        Assert.NotEmpty(context.Sources);
        Assert.Contains(context.Sources, s => s.SourceName == "Support Contact");
    }

    private static ClaimsPrincipal BuildPrincipal(string userId, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth");

        return new ClaimsPrincipal(identity);
    }
}
