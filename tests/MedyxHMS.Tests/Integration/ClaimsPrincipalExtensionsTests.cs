using System.Security.Claims;
using MedyxHMS.Extensions;
using Xunit;

namespace MedyxHMS.Tests.Integration;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void HasPermission_ShouldAllowConfiguredRoleActionPairs()
    {
        var user = CreatePrincipal("Receptionist");

        Assert.True(user.HasPermission("Patient", "Add"));
        Assert.True(user.HasPermission("Appointment", "Edit"));
        Assert.False(user.HasPermission("Patient", "Delete"));
    }

    [Fact]
    public void HasPermission_ShouldDenyPatientsForStaffManagementActions()
    {
        var user = CreatePrincipal("Patient");

        Assert.False(user.HasPermission("Patient", "View"));
        Assert.False(user.HasPermission("Appointment", "Add"));
        Assert.False(user.HasPermission("ManageUsers", "Edit"));
    }

    [Fact]
    public void HasPermission_ShouldFallbackToAdminRolesForUnknownModules()
    {
        var admin = CreatePrincipal("Admin");
        var doctor = CreatePrincipal("Doctor");

        Assert.True(admin.HasPermission("UnknownModule", "View"));
        Assert.False(doctor.HasPermission("UnknownModule", "View"));
    }

    private static ClaimsPrincipal CreatePrincipal(params string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user"));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
