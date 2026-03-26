using FluentValidation;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.UserReportingLines.Requests;
using sfa_api.Features.UserReportingLines.Services;
using sfa_api.Features.UserReportingLines.Validators;

namespace sfa_api.Features.UserReportingLines;

public static class UserReportingLinesServiceExtensions
{
    public static IServiceCollection AddUserReportingLinesFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserReportingLineRepository, UserReportingLineRepository>();
        services.AddScoped<IUserReportingLineService, UserReportingLineService>();
        services.AddScoped<IValidator<CreateUserReportingLineRequest>, CreateUserReportingLineValidator>();
        services.AddScoped<IValidator<UpdateUserReportingLineRequest>, UpdateUserReportingLineValidator>();
        return services;
    }
}
