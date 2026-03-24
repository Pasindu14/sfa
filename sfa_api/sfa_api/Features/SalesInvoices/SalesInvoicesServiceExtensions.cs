using FluentValidation;
using sfa_api.Features.SalesInvoices.Repositories;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.SalesInvoices.Services;
using sfa_api.Features.SalesInvoices.Validators;

namespace sfa_api.Features.SalesInvoices;

public static class SalesInvoicesServiceExtensions
{
    public static IServiceCollection AddSalesInvoicesFeature(this IServiceCollection services)
    {
        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepository>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        services.AddScoped<IValidator<ImportSalesInvoicesRequest>, ImportSalesInvoicesValidator>();
        return services;
    }
}
