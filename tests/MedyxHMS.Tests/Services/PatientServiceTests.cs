using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class PatientServiceTests
{
    [Fact]
    public async Task CreatePatientAsync_ShouldAssignGeneratedPatientIdAndSetActive()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var service = new PatientService(context);

        var created = await service.CreatePatientAsync(ModelFactory.CreatePatient());

        Assert.NotNull(created.PatientId);
        Assert.StartsWith($"PTN{DateTime.UtcNow.Year}", created.PatientId);
        Assert.True(created.IsActive);
        Assert.True(created.CreatedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SearchPatientsAsync_ShouldReturnOnlyMatchingActivePatients()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var service = new PatientService(context);

        var activeMatch = ModelFactory.CreatePatient("Alice", "Walker");
        var activeOther = ModelFactory.CreatePatient("Bob", "Stone");
        var inactiveMatch = ModelFactory.CreatePatient("Alice", "Inactive");
        inactiveMatch.IsActive = false;

        context.Patients.AddRange(activeMatch, activeOther, inactiveMatch);
        await context.SaveChangesAsync();

        var results = (await service.SearchPatientsAsync("Alice")).ToList();

        Assert.Single(results);
        Assert.Equal("Alice", results[0].FirstName);
        Assert.True(results[0].IsActive);
    }

    [Fact]
    public async Task DeletePatientAsync_ShouldSoftDeletePatient()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);
        var service = new PatientService(context);

        var patient = ModelFactory.CreatePatient();
        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var deleted = await service.DeletePatientAsync(patient.Id);
        var stored = await context.Patients.FindAsync(patient.Id);

        Assert.True(deleted);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
    }
}
