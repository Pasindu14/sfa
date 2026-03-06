using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace sfa_api.Common.Audit;

public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = httpContext?.Items["CorrelationId"]?.ToString();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

        // Get user ID from JWT token
        var userIdClaim = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var changedBy = Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;

        var excluded = new[] { "Password", "PasswordHash", "RefreshToken", "Pin", "Token" };

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                && e.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
            .ToList();

        var auditEntries = entries.Select(e =>
        {
            var oldValues = e.State == EntityState.Added ? null
                : JsonSerializer.Serialize(
                    e.Properties
                        .Where(p => !excluded.Any(x =>
                            p.Metadata.Name.Contains(x,
                                StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));

            var newValues = e.State == EntityState.Deleted ? null
                : JsonSerializer.Serialize(
                    e.Properties
                        .Where(p => !excluded.Any(x =>
                            p.Metadata.Name.Contains(x,
                                StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));

            return new AuditLog
            {
                EntityType = e.Entity.GetType().Name,
                EntityId = e.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())
                    ?.CurrentValue?.ToString() ?? string.Empty,
                Operation = e.State switch
                {
                    EntityState.Added => "CREATE",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                },
                OldValues = oldValues,
                NewValues = newValues,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                IpAddress = ipAddress
            };
        });

        context.Set<AuditLog>().AddRange(auditEntries);
    }
}
