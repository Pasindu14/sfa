using FluentValidation;
using sfa_api.Features.GRNs.Repositories;
using sfa_api.Features.GRNs.Services;
using sfa_api.Features.GRNs.Validators;

namespace sfa_api.Features.GRNs;

public static class GrnsServiceExtensions
{
    public static IServiceCollection AddGrnsFeature(this IServiceCollection services)
    {
        services.AddScoped<IGrnRepository, GrnRepository>();
        services.AddScoped<IGrnService, GrnService>();
        services.AddValidatorsFromAssemblyContaining<CreateGrnValidator>(includeInternalTypes: false);
        return services;
    }
}
