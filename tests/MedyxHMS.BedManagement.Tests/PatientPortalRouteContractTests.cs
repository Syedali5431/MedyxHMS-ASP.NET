using System.Reflection;
using MedyxHMS.Controllers.PatientPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

public class PatientPortalRouteContractTests
{
    [Theory]
    [InlineData(typeof(DashboardController))]
    [InlineData(typeof(AppointmentsController))]
    [InlineData(typeof(BillsController))]
    [InlineData(typeof(MedicalRecordsController))]
    [InlineData(typeof(SettingsController))]
    public void PatientPortalControllers_RequirePatientRole(Type controllerType)
    {
        var authorize = controllerType.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Patient", authorize!.Roles);
    }

    [Theory]
    [InlineData(typeof(DashboardController), "PatientPortal/[controller]/[action]")]
    [InlineData(typeof(AppointmentsController), "PatientPortal/[controller]/[action]")]
    [InlineData(typeof(BillsController), "PatientPortal/[controller]/[action]")]
    [InlineData(typeof(MedicalRecordsController), "PatientPortal/[controller]/[action]")]
    [InlineData(typeof(SettingsController), "PatientPortal/[controller]/[action]")]
    public void PatientPortalControllers_UseExpectedRouteTemplate(Type controllerType, string expectedTemplate)
    {
        var route = controllerType.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(route);
        Assert.Equal(expectedTemplate, route!.Template);
    }

    [Theory]
    [InlineData(typeof(DashboardController))]
    [InlineData(typeof(AppointmentsController))]
    [InlineData(typeof(BillsController))]
    [InlineData(typeof(MedicalRecordsController))]
    [InlineData(typeof(SettingsController))]
    public void PatientPortalControllers_AreInPatientPortalArea(Type controllerType)
    {
        var area = controllerType.GetCustomAttribute<AreaAttribute>();

        Assert.NotNull(area);
        Assert.Equal("PatientPortal", area!.RouteValue);
    }
}