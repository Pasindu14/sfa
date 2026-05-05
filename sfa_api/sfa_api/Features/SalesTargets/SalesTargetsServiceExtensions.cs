using FluentValidation;
using sfa_api.Features.SalesTargets.Repositories;
using sfa_api.Features.SalesTargets.Requests;
using sfa_api.Features.SalesTargets.Services;
using sfa_api.Features.SalesTargets.Validators;

namespace sfa_api.Features.SalesTargets;

public static class SalesTargetsServiceExtensions
{
    public static IServiceCollection AddSalesTargetsFeature(this IServiceCollection services)
    {
        services.AddScoped<ISalesTargetRepository, SalesTargetRepository>();
        services.AddScoped<ISalesTargetImportBatchRepository, SalesTargetImportBatchRepository>();
        services.AddScoped<ISalesTargetService, SalesTargetService>();
        services.AddScoped<ISalesTargetImportService, SalesTargetImportService>();
        services.AddScoped<IValidator<ImportSalesTargetsRequest>, ImportSalesTargetsRequestValidator>();
        services.AddScoped<IValidator<UpdateSalesTargetRequest>, UpdateSalesTargetRequestValidator>();
        return services;
    }
}
