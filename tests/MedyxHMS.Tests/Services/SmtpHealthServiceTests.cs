using MedyxHMS.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MedyxHMS.Tests.Services;

public class SmtpHealthServiceTests
{
    [Fact]
    public async Task CheckAsync_ShouldReportMissingConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Notification:Smtp:Host"] = "",
                ["Notification:Smtp:Port"] = "abc",
                ["Notification:Smtp:FromEmail"] = ""
            })
            .Build();

        var service = new SmtpHealthService(config);
        var result = await service.CheckAsync();

        Assert.False(result.IsConfigured);
        Assert.False(result.ConnectivityOk);
        Assert.Contains(result.Issues, issue => issue.Contains("host", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Issues, issue => issue.Contains("from-email", StringComparison.OrdinalIgnoreCase));
    }
}
