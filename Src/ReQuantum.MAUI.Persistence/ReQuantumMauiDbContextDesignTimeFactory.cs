using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NOF.Infrastructure.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore.SQLite;

namespace ReQuantum.MAUI.Persistence;

public class ReQuantumMauiDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ReQuantumMauiDbContext>
{
    public ReQuantumMauiDbContext CreateDbContext(string[] args)
    {
        var builder = MigratorAppBuilder.Create(args);
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:sqlite"] = "Data Source=requantum.db"

            });
        builder.AddEFCore<ReQuantumMauiDbContext>()
            .UseSqlite();
        var app = builder.BuildAsync().GetAwaiter().GetResult();
        return app.Services.CreateScope().ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
    }
}
