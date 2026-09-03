using FluentValidation;
using sfa_api.Features.Reports.Repositories;
using sfa_api.Features.Reports.Services;
using sfa_api.Features.Reports.Validators;

namespace sfa_api.Features.Reports;

public static class ReportsServiceExtensions
{
    public static IServiceCollection AddReportsFeature(this IServiceCollection services)
    {
        services.AddScoped<ISalesSummaryRepository, SalesSummaryRepository>();
        services.AddScoped<ISalesSummaryService, SalesSummaryService>();
        services.AddValidatorsFromAssemblyContaining<SalesSummaryQueryValidator>(includeInternalTypes: false);
        return services;
    }
}
