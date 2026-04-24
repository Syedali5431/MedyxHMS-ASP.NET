using System.Reflection;
using MedyxHMS.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

public class BedManagementAuthorizationTests
{
    private const string ViewRoles = "SuperAdmin,Admin,Doctor,Nurse,Pharmacist,Accountant,Receptionist,LabTechnician,Radiologist,Staff";
    private const string ManageRoles = "SuperAdmin,Admin,Nurse";

    [Fact]
    public void Controller_UsesViewRolesAtClassLevel()
    {
        var attribute = typeof(BedManagementController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(ViewRoles, attribute!.Roles);
    }

    [Fact]
    public void MutatingEndpoints_RequireManageRoles()
    {
        AssertRequiresManageRoles(nameof(BedManagementController.Assign), typeof(int), typeof(int));
        AssertRequiresManageRoles(nameof(BedManagementController.Release), typeof(int));
        AssertRequiresManageRoles(nameof(BedManagementController.Transfer), typeof(int), typeof(int));
        AssertRequiresManageRoles(nameof(BedManagementController.SetStatus), typeof(int), typeof(string));
        AssertRequiresManageRoles(nameof(BedManagementController.AssignBedApi), typeof(BedManagementController.AssignBedRequest));
        AssertRequiresManageRoles(nameof(BedManagementController.ReleaseBedApi), typeof(BedManagementController.ReleaseBedRequest));
        AssertRequiresManageRoles(nameof(BedManagementController.TransferBedApi), typeof(BedManagementController.TransferBedRequest));
        AssertRequiresManageRoles(nameof(BedManagementController.UpdateBedStatusApi), typeof(BedManagementController.UpdateBedStatusRequest));
        AssertRequiresManageRoles(nameof(BedManagementController.Create));
        AssertRequiresManageRoles(nameof(BedManagementController.Create), typeof(MedyxHMS.Models.Bed));
        AssertRequiresManageRoles(nameof(BedManagementController.Edit), typeof(int));
        AssertRequiresManageRoles(nameof(BedManagementController.Edit), typeof(int), typeof(MedyxHMS.Models.Bed));
    }

    [Fact]
    public void ReadOnlyEndpoints_DoNotAddManageRoleOverride()
    {
        AssertHasNoMethodAuthorize(nameof(BedManagementController.Index));
        AssertHasNoMethodAuthorize(nameof(BedManagementController.GetBedsApi));
    }

    private static void AssertRequiresManageRoles(string methodName, params Type[] parameterTypes)
    {
        var method = typeof(BedManagementController).GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(ManageRoles, authorize!.Roles);
    }

    private static void AssertHasNoMethodAuthorize(string methodName, params Type[] parameterTypes)
    {
        var method = typeof(BedManagementController).GetMethod(methodName, parameterTypes);

        Assert.NotNull(method);
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.Null(authorize);
    }
}