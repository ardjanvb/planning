using Microsoft.Extensions.DependencyInjection;
using Services.Contracts;
using Services.Export;
using Services.Implementations;
using Services.Import;

namespace Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<IExportService, ExportService>();
        return services;
    }
}
