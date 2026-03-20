using FluentValidation;
using sfa_api.Common.Errors;
using ValidationException = sfa_api.Common.Errors.ValidationException;

namespace sfa_api.Common.Extensions;

public static class ValidatorExtensions
{
    public static async Task ValidateOrThrowAsync<T>(
        this IValidator<T> validator,
        T request,
        CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var fields = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(fields);
        }
    }
}
