using FluentValidation;
using sfa_api.Features.Areas.Repositories;
using sfa_api.Features.Areas.Requests;
using sfa_api.Features.Areas.Services;
using sfa_api.Features.Areas.Validators;

namespace sfa_api.Features.Areas;

public static class AreasServiceExtensions
{
    public static IServiceCollection AddAreasFeature(this IServiceCollection services)
    {
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<IValidator<CreateAreaRequest>, CreateAreaValidator>();
        services.AddScoped<IValidator<UpdateAreaRequest>, UpdateAreaValidator>();
        return services;
    }
}
