using FluentMigrator.Runner;

namespace Hl7Hackathon.Web.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Runs all pending migrations, including the seed migrations when <c>AppSettings.SeedDatabase</c> is true.
    /// </summary>
    public static void RunMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp();
    }
}
