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

    [Fact]
    public void EvaluateOutput_ShouldBlockLowConfidenceOutput()
    {
        var service = new ChatbotModerationService();

        var result = service.EvaluateOutput("You should take antibiotics now.", 1, 0.20m);

        Assert.True(result.IsBlocked);
        Assert.Equal("LowConfidenceOutput", result.Reason);
    }

    [Fact]
    public void EvaluateOutput_ShouldAllowGroundedOperationalOutput()
    {
        var service = new ChatbotModerationService();

        var result = service.EvaluateOutput("You can review pending bills in the Billing module. Source: Billing Help", 1, 0.85m);

        Assert.False(result.IsBlocked);
        Assert.Equal("Allowed", result.Reason);
    }
}
