using System.Text;
using MedyxHMS.Data;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

public class PatientRecordsTests
{
    [Fact]
    public async Task OpdVisit_CreatesUpdatesAndRemovesItsMedicalRecord()
    {
        await using var context = await SeedAsync();
        var service = new OPDService(context);

        var visit = await service.CreateOPDVisitAsync(new OPDVisit
        {
            PatientId = 1, DoctorId = 1, VisitDate = new DateTime(2026, 9, 1, 10, 30, 0),
            Symptoms = "Fever", Diagnosis = "Viral fever", Treatment = "Rest", Prescription = "Paracetamol 500 mg", Notes = "Review in 3 days"
        });
        var record = await context.MedicalRecords.SingleAsync();
        Assert.Equal(("OPD", MedicalRecordService.OpdSource, visit.Id), (record.RecordType, record.SourceType, record.SourceId!.Value));
        Assert.Equal("OPD consultation – Fever", record.Description);
        Assert.Equal("Viral fever", record.Diagnosis);
        Assert.Contains("Prescribed: Paracetamol 500 mg", record.Treatment);
        Assert.Equal("Asha Rao", record.DoctorName);
        Assert.Null(record.DoctorId); // the doctor has no login

        visit.Diagnosis = "Dengue";
        await service.UpdateOPDVisitAsync(visit);
        Assert.Equal("Dengue", (await context.MedicalRecords.AsNoTracking().SingleAsync()).Diagnosis);

        await service.DeleteOPDVisitAsync(visit.Id);
        Assert.Empty(await context.MedicalRecords.ToListAsync());
    }

    [Fact]
    public async Task EnsureRecords_BackfillsVisitsAndAdmissionsOnce_AndLinksDoctorLogin()
    {
        await using var context = await SeedAsync();
        context.Users.Add(new ApplicationUser { Id = "user-1", UserName = "dr.rao", Email = "asha.rao@example.test", EmployeeId = "DOC1", FirstName = "Asha", LastName = "Rao" });
        context.OPDVisits.Add(new OPDVisit { Id = 5, PatientId = 1, DoctorId = 1, VisitDate = new DateTime(2026, 8, 1), Diagnosis = "Migraine" });
        context.IPDAdmissions.Add(new IPDAdmission { Id = 7, PatientId = 1, DoctorId = 1, AdmissionDate = new DateTime(2026, 8, 3), DischargeDate = new DateTime(2026, 8, 6), AdmissionType = "Emergency", Status = "Discharged", Diagnosis = "Appendicitis" });
        await context.SaveChangesAsync();
        var records = new MedicalRecordService(context);

        Assert.Equal(2, await records.EnsureRecordsAsync());
        Assert.Equal(0, await records.EnsureRecordsAsync());

        var ipd = await context.MedicalRecords.SingleAsync(m => m.RecordType == "IPD");
        Assert.Equal("Emergency admission on 03-Aug-2026, discharged 06-Aug-2026", ipd.Description);
        Assert.Equal("user-1", ipd.DoctorId);
        Assert.Equal(7, ipd.SourceId);
    }

    [Fact]
    public async Task DocumentService_ChecksTypeAndContent_AndFingerprintsTheFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "medyx-doc-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new PatientDocumentService(new TestEnvironment(root));
            var fakePdf = await service.SaveAsync(File("report.pdf", Encoding.ASCII.GetBytes("<html><script>alert(1)</script></html>")));
            var exe = await service.SaveAsync(File("setup.exe", new byte[] { 0x4D, 0x5A, 0x90, 0x00 }));
            var png = await service.SaveAsync(File("scan.png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 }));

            Assert.Null(fakePdf.File);
            Assert.Contains("does not match", fakePdf.Error);
            Assert.Null(exe.File);
            Assert.Contains("Allowed file types", exe.Error);
            Assert.NotNull(png.File);
            Assert.Equal("image/png", png.File!.ContentType);
            Assert.Equal(64, png.File.Sha256.Length);
            Assert.NotNull(service.GetPath(png.File.StoredFileName));
            Assert.Null(service.GetPath("..\\..\\appsettings.json"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task UpdatingAPostedRecord_KeepsItsCreationDate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var created = new DateTime(2026, 8, 14, 6, 26, 0);
        await using (var context = new ApplicationDbContext(options))
        {
            context.InventoryItems.Add(new InventoryItem { Id = 1, ItemCode = "GL-1", Name = "Gloves", CreatedDate = created });
            await context.SaveChangesAsync();
        }

        // An edit form posts the record without its creation date (the model default is "now").
        await using (var context = new ApplicationDbContext(options))
        {
            context.InventoryItems.Update(new InventoryItem { Id = 1, ItemCode = "GL-1", Name = "Gloves (box)" });
            await context.SaveChangesAsync();
        }

        await using (var context = new ApplicationDbContext(options))
        {
            var item = await context.InventoryItems.SingleAsync();
            Assert.Equal("Gloves (box)", item.Name);
            Assert.Equal(created, item.CreatedDate);
        }
    }

    private static IFormFile File(string name, byte[] content) => new FormFile(new MemoryStream(content), 0, content.Length, "file", name);

    private static async Task<ApplicationDbContext> SeedAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new ApplicationDbContext(options);
        context.Patients.Add(new Patient { Id = 1, FirstName = "Pat", LastName = "One" });
        context.Doctors.Add(new Doctor { Id = 1, FirstName = "Asha", LastName = "Rao", EmployeeId = "DOC1", Email = "asha.rao@example.test" });
        await context.SaveChangesAsync();
        return context;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public TestEnvironment(string root) { ContentRootPath = root; WebRootPath = root; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "MedyxHMS.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "Test";
    }
}
