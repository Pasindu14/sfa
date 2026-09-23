using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.IntegrationTests.Infrastructure;

/// <summary>
/// Every enum stored as a string must fit its column. SQLite ignores varchar lengths, so a
/// too-long member name only fails on PostgreSQL (22001) — e.g. "StockTakingAdjustment" (21)
/// in the old varchar(20) StockTransactions.TransactionType broke every stock-taking adjustment.
/// </summary>
[Collection(SfaApiCollection.Name)]
public class EnumColumnLengthTests(SfaWebApplicationFactory factory)
{
    [Fact]
    public void EveryStringEnumMemberName_FitsItsColumnMaxLength()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var violations = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (!enumType.IsEnum) continue;
            if (property.GetProviderClrType() != typeof(string)) continue;

            var maxLength = property.GetMaxLength();
            if (maxLength is null) continue;

            foreach (var name in Enum.GetNames(enumType).Where(n => n.Length > maxLength))
                violations.Add($"{entity.ClrType.Name}.{property.Name}: '{name}' ({name.Length}) > {maxLength}");
        }

        violations.Should().BeEmpty();
    }
}
