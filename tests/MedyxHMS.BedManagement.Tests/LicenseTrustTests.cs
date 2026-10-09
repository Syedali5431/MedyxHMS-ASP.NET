using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedyxHMS.Data;
using MedyxHMS.Services.Implementations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MedyxHMS.BedManagement.Tests;

// Licences are accepted only from the vendor keys listed in LicenseTrust.
public class LicenseTrustTests
{
    [Fact]
    public void ShippedPublicKey_IsATrustedVendorKey()
    {
        var keyFile = Directory.GetFiles(Path.Combine(RepositoryRoot(), "MedyxHMS-Lic", "current"), "medyxhms-public-key-*.json").Single();
        using var json = JsonDocument.Parse(File.ReadAllText(keyFile));
        var verificationKey = LicenseCryptoUtility.ComputeVerificationKey(
            json.RootElement.GetProperty("ModulusHex").GetString()!, json.RootElement.GetProperty("ExponentHex").GetString()!);

        Assert.True(LicenseTrust.IsTrusted(verificationKey));
    }

    [Theory]
    [InlineData("04AD685B27D1F24D981D248E2A7FC132B2CFCFE12D1221552F505F7EA70B5908")] // replaced vendor key (private key was exposed)
    [InlineData("")]
    [InlineData(null)]
    public void ReplacedOrMissingKeys_AreNotTrusted(string? verificationKey)
    {
        Assert.False(LicenseTrust.IsTrusted(verificationKey));
    }

    [Fact]
    public async Task Licence_SignedWithAnotherKey_IsRefused()
    {
        using var rsa = RSA.Create(2048);
        var p = rsa.ExportParameters(false);
        var verificationKey = LicenseCryptoUtility.ComputeVerificationKey(Convert.ToHexString(p.Modulus!), Convert.ToHexString(p.Exponent!));
        var licence = new
        {
            Payload = new
            {
                ProductName = "MedyxHMS", TenantId = "UNCONFIGURED", LicenseId = Guid.NewGuid(),
                IssuedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddYears(1), MaxConcurrentUsers = 10,
                VerificationKey = verificationKey, LicensedModules = new[] { "Dashboard" }, Nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            },
            Algorithm = "RSA-SHA256",
            SignatureHex = "00",
        };
        var bytes = Encoding.UTF8.GetBytes("MEDYX-LIC-V1:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(licence))));
        using var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "licenseFile", "MedyxHMS.lic");

        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new LicenseFileService(context, null!, new EphemeralDataProtectionProvider(), null!);

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => service.ValidateAndActivateAsync(file, null));
        Assert.Equal(LicenseTrust.UntrustedKeyMessage, ex.Message);
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MedyxHMS.csproj")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("MedyxHMS.csproj not found above the test folder.");
    }
}
