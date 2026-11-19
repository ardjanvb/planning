using Domain.Contexts;
using Domain.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domain.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=planning.db";

        services.AddDbContext<RepositoryContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    public static void EnsureDatabaseCreated(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
        context.Database.EnsureCreated();
        EnsureSchemaPatches(context);
        DemoSeed.EnsureSeeded(context);
    }

    private static void EnsureSchemaPatches(RepositoryContext context)
    {
        // EnsureCreated does not add new tables to an existing database.
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS "ShiftClosures" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ShiftClosures" PRIMARY KEY AUTOINCREMENT,
                "Date" TEXT NOT NULL,
                "ShiftName" TEXT NOT NULL,
                "Remark" TEXT NULL
            );
            """);
        context.Database.ExecuteSqlRaw(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ShiftClosures_Date_ShiftName"
            ON "ShiftClosures" ("Date", "ShiftName");
            """);
    }
}
