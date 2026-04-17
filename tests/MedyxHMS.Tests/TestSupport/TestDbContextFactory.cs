using MedyxHMS.Data;
using Microsoft.EntityFrameworkCore;

namespace MedyxHMS.Tests.TestSupport;

internal static class TestDbContextFactory
{
    public static ApplicationDbContext Create(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        return new ApplicationDbContext(options);
    }
}
