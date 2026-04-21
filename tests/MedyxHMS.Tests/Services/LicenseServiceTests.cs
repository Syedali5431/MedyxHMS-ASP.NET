using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class LicenseServiceTests
{
    [Fact]
    public async Task GetCurrentSnapshotAsync_ShouldMarkLicenseAsExpiringSoon()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.LicenseRecords.Add(new LicenseRecord
        {
            LicenseReference = "LIC-001",
            ExpiresAtUtc = DateTime.UtcNow.Date.AddDays(3),
            Status = LicenseState.Active.ToString(),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var settings = new FakeSettingService();
        var service = new LicenseService(context, new FakeEmailNotificationProvider(), settings, new FakeLicenseFileService(), NullLogger<LicenseService>.Instance);

        var snapshot = await service.GetCurrentSnapshotAsync();

        Assert.Equal(LicenseState.ExpiringSoon, snapshot.State);
        Assert.Equal(3, snapshot.DaysRemaining);
    }

    [Fact]
    public async Task RenewAsync_ShouldRejectManualRenewalWorkflow()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.LicenseRecords.Add(new LicenseRecord
        {
            LicenseReference = "LIC-002",
            ExpiresAtUtc = DateTime.UtcNow.Date.AddDays(10),
            Status = LicenseState.Active.ToString(),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new LicenseService(context, new FakeEmailNotificationProvider(), new FakeSettingService(), new FakeLicenseFileService(), NullLogger<LicenseService>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenewAsync(4, "superadmin-user-id"));
        Assert.Contains("Manual renewal is disabled", exception.Message);
    }

    [Fact]
    public async Task SendReminderAsync_ShouldSendOnlyOncePerExpiryCycle()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.LicenseRecords.Add(new LicenseRecord
        {
            LicenseReference = "LIC-003",
            ExpiresAtUtc = DateTime.UtcNow.Date.AddDays(5),
            Status = LicenseState.Active.ToString(),
            IsActive = true
        });
        context.Users.AddRange(
            new ApplicationUser { Id = "u1", Email = "alpha@example.com", UserName = "alpha@example.com", EmployeeId = "EMP001", FirstName = "Alpha", LastName = "Admin", IsActive = true },
            new ApplicationUser { Id = "u2", Email = "beta@example.com", UserName = "beta@example.com", EmployeeId = "EMP002", FirstName = "Beta", LastName = "Staff", IsActive = true },
            new ApplicationUser { Id = "u3", Email = string.Empty, UserName = "gamma@example.com", EmployeeId = "EMP003", FirstName = "Gamma", LastName = "User", IsActive = true });
        context.Set<IdentityRole>().Add(new IdentityRole("SuperAdmin") { Id = "role-superadmin" });
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = "u1", RoleId = "role-superadmin" });
        await context.SaveChangesAsync();

        var emailProvider = new FakeEmailNotificationProvider();
        var settings = new FakeSettingService();
        var service = new LicenseService(context, emailProvider, settings, new FakeLicenseFileService(), NullLogger<LicenseService>.Instance);

        var first = await service.SendReminderAsync(force: false, performedByUserId: "u1");
        var second = await service.SendReminderAsync(force: false, performedByUserId: "u1");

        Assert.Equal("Sent", first.Status);
        Assert.Equal(2, first.SentToCount);
        Assert.Equal("Skipped", second.Status);
        Assert.Equal(2, emailProvider.Sent.Count);
        Assert.Single(context.LicenseReminderLogs);
    }

    [Fact]
    public async Task ShouldRestrictAccessAsync_ShouldBlockExpiredStaffButAllowExemptRequests()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        context.LicenseRecords.Add(new LicenseRecord
        {
            LicenseReference = "LIC-004",
            ExpiresAtUtc = DateTime.UtcNow.Date.AddDays(-1),
            Status = LicenseState.Active.ToString(),
            IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new LicenseService(context, new FakeEmailNotificationProvider(), new FakeSettingService(), new FakeLicenseFileService(), NullLogger<LicenseService>.Instance);

        var staffPrincipal = CreatePrincipal("staff-1", "Staff");
        var superAdminPrincipal = CreatePrincipal("admin-1", "SuperAdmin");

        Assert.True(await service.ShouldRestrictAccessAsync(staffPrincipal, "/Dashboard/Index"));
        Assert.False(await service.ShouldRestrictAccessAsync(staffPrincipal, "/PatientPortal/Dashboard"));
        Assert.False(await service.ShouldRestrictAccessAsync(superAdminPrincipal, "/Dashboard/Index"));
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}