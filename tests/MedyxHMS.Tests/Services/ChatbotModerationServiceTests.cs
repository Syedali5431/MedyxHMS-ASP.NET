using MedyxHMS.Services.Implementations;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class ChatbotModerationServiceTests
{
    [Fact]
    public void Evaluate_ShouldBlockEmergencyContent()
    {
        var service = new ChatbotModerationService();

        var result = service.Evaluate("I have severe chest pain and can't breathe.");

        Assert.True(result.IsBlocked);
        Assert.True(result.NeedsEmergencyEscalation);
        Assert.Equal("EmergencyEscalation", result.Reason);
    }

    [Fact]
    public void Evaluate_ShouldBlockUnsafeMedicalAdviceRequest()
    {
        var service = new ChatbotModerationService();

        var result = service.Evaluate("Please diagnose my symptoms and suggest dosage.");

        Assert.True(result.IsBlocked);
        Assert.False(result.NeedsEmergencyEscalation);
        Assert.Equal("UnsafeMedicalAdviceRequest", result.Reason);
    }

    [Fact]
    public void Evaluate_ShouldAllowOperationalQuestion()
    {
        var service = new ChatbotModerationService();

        var result = service.Evaluate("Where can I check pending bills?");

        Assert.False(result.IsBlocked);
        Assert.Equal("Allowed", result.Reason);
    }
}
