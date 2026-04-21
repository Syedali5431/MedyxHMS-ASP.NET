using System.Security.Claims;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Services.Interfaces;
using MedyxHMS.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class ChatbotOperationsStep73Tests
{
    [Fact]
    public async Task GetAnalyticsAsync_ShouldReturnExpectedRatesAndCounts()
    {
        using var db = TestDbContextFactory.Create(nameof(GetAnalyticsAsync_ShouldReturnExpectedRatesAndCounts));

        var now = DateTime.UtcNow;
        db.ChatSessions.AddRange(
            new ChatSession { Id = "s1", UserId = "u1", UserRole = "Patient", StartedAtUtc = now.AddDays(-1), Status = "Active", Channel = "Web", IsUnresolved = true },
            new ChatSession { Id = "s2", UserId = "u2", UserRole = "Staff", StartedAtUtc = now.AddDays(-1), Status = "Active", Channel = "Web" });

        db.ChatMessages.AddRange(
            new ChatMessage { SessionId = "s1", SenderType = "User", Content = "help appointment", Category = "Appointment", ModerationStatus = "Allowed", TokenCount = 1, CreatedAtUtc = now.AddDays(-1) },
            new ChatMessage { SessionId = "s2", SenderType = "User", Content = "show bill", Category = "Billing", ModerationStatus = "Allowed", TokenCount = 1, CreatedAtUtc = now.AddDays(-1) });

        db.ChatEscalations.Add(new ChatEscalation
        {
            SessionId = "s1",
            UserId = "u1",
            EscalationType = "Support",
            Reason = "Need help",
            Status = "Pending",
            CreatedAtUtc = now.AddHours(-12)
        });

        await db.SaveChangesAsync();

        var sut = BuildService(db);

        var snapshot = await sut.GetAnalyticsAsync(7);

        Assert.Equal(2, snapshot.TotalSessions);
        Assert.Equal(2, snapshot.TotalMessages);
        Assert.Equal(1, snapshot.TotalEscalations);
        Assert.Equal(1, snapshot.UnresolvedSessions);
        Assert.Equal(0.5m, snapshot.EscalationRate);
        Assert.Equal(0.5m, snapshot.UnresolvedRate);
        Assert.True(snapshot.CategoryCounts.ContainsKey("Appointment"));
        Assert.True(snapshot.CategoryCounts.ContainsKey("Billing"));
    }

    [Fact]
    public async Task EscalateAsync_ShouldCreatePendingEscalationForOwnedSession()
    {
        using var db = TestDbContextFactory.Create(nameof(EscalateAsync_ShouldCreatePendingEscalationForOwnedSession));

        db.ChatSessions.Add(new ChatSession
        {
            Id = "owned-session",
            UserId = "user-1",
            UserRole = "Patient",
            StartedAtUtc = DateTime.UtcNow,
            Status = "Active",
            Channel = "Web"
        });
        await db.SaveChangesAsync();

        var sut = BuildService(db);
        var user = BuildPrincipal("user-1", "Patient");

        var escalation = await sut.EscalateAsync(user, "owned-session", null, "Please connect me with support.");

        Assert.NotNull(escalation);
        Assert.Equal("Pending", escalation!.Status);
        Assert.Equal("user-1", escalation.UserId);
        Assert.True(db.ChatEscalations.Any(e => e.Id == escalation.Id));
    }

    [Fact]
    public async Task UpdateAdminSettingsAsync_ShouldPersistAndReturnUpdatedValues()
    {
        using var db = TestDbContextFactory.Create(nameof(UpdateAdminSettingsAsync_ShouldPersistAndReturnUpdatedValues));
        var sut = BuildService(db);

        var updated = await sut.UpdateAdminSettingsAsync(new ChatbotAdminSettings
        {
            Enabled = true,
            EnableEscalation = true,
            EnableAppointmentGuidance = true,
            EnableBillingGuidance = true,
            EnableMultilingual = true,
            EnabledForPatients = true,
            EnabledForStaff = true,
            EnabledForAdmins = true,
            Model = "gpt-4o-mini",
            Temperature = 0.3m,
            MaxTokens = 420,
            UnresolvedThreshold = 0.4m,
            HourlyUsageLimit = 120,
            SupportedLanguagesCsv = "en,es",
            DefaultLanguage = "en"
        }, "admin-1");

        Assert.True(updated);

        var current = await sut.GetAdminSettingsAsync();
        Assert.True(current.EnableMultilingual);
        Assert.Equal(420, current.MaxTokens);
        Assert.Equal("en,es", current.SupportedLanguagesCsv);
    }

    private static OpenAiChatbotService BuildService(MedyxHMS.Data.ApplicationDbContext db)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenAI:Enabled"] = "false",
            ["OpenAI:Model"] = "gpt-4o-mini"
        }).Build();

        return new OpenAiChatbotService(
            db,
            new FakeHttpClientFactory(),
            config,
            new FakeSettingService(),
            new ChatbotModerationService(),
            new ChatbotPiiRedactionService(),
            new ChatbotPromptBuilder(),
            new ChatbotKnowledgeService(db, new FakeSettingService()),
            NullLogger<OpenAiChatbotService>.Instance);
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

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
