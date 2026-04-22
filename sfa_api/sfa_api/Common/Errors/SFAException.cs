namespace sfa_api.Common.Errors;

public abstract class SFAException(string errorCode, string message, object? data = null) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
    public new object? Data { get; } = data;
}

// 400 — Validation
public class ValidationException(Dictionary<string, string[]> fields) : SFAException("VALIDATION_FAILED", "One or more validation errors occurred.")
{
    public Dictionary<string, string[]> Fields { get; } = fields;
}

// 401 — Authentication
public class AuthenticationException(string code, string message) : SFAException(code, message)
{
}
public class TokenExpiredException : AuthenticationException
{
    public TokenExpiredException()
        : base("AUTH_TOKEN_EXPIRED", "Access token has expired.") { }
}
public class InvalidTokenException : AuthenticationException
{
    public InvalidTokenException()
        : base("AUTH_INVALID_TOKEN", "Token is invalid or has been revoked.") { }
}

// 403 — Authorization
public class AuthorizationException(string resource) : SFAException("FORBIDDEN_ACCESS", $"You do not have permission to access {resource}.")
{
}

// 404 — Not Found
public class NotFoundException(string entity, object id) : SFAException($"{entity.ToUpperInvariant()}_NOT_FOUND",
        $"{entity} with ID '{id}' was not found.")
{
}

// 409 — Conflict
public class ConflictException : SFAException
{
    protected ConflictException(string code, string message, object? currentData = null)
        : base(code, message, currentData) { }
}
public class ConcurrencyConflictException(object? currentData = null) : ConflictException("CONCURRENCY_CONFLICT", "Record was modified by another user.", currentData)
{
}
public class DuplicateResourceException(string entity) : ConflictException($"{entity.ToUpperInvariant()}_DUPLICATE", $"{entity} already exists.")
{
}

// 422 — Business Rule
public class BusinessRuleException : SFAException
{
    public BusinessRuleException(string code, string message, object? data = null)
        : base(code, message, data) { }
}

public record StockShortage(int ProductId, string ProductName, decimal Requested, decimal Available);

public class InsufficientStockException : BusinessRuleException
{
    public IReadOnlyList<StockShortage> Shortages { get; }
    public Dictionary<string, string[]> Fields { get; }

    public InsufficientStockException(IReadOnlyList<StockShortage> shortages)
        : base(
            "INSUFFICIENT_STOCK",
            shortages.Count == 1
                ? $"No stock of '{shortages[0].ProductName}' (requested {shortages[0].Requested}, available {shortages[0].Available})."
                : $"{shortages.Count} products are out of stock.",
            new { shortages })
    {
        Shortages = shortages;
        Fields = shortages.ToDictionary(
            s => $"product:{s.ProductId}",
            s => new[] { $"{s.ProductName}: you ordered {s.Requested}, only {s.Available} available" });
    }
}
public class InvalidOrderStateException(string currentState, string attemptedTransition) : BusinessRuleException("INVALID_ORDER_STATE",
        $"Cannot transition order from '{currentState}' to '{attemptedTransition}'.",
        new { currentState, attemptedTransition })
{
}
public class LeadAlreadyConvertedException(Guid leadId) : BusinessRuleException("LEAD_ALREADY_CONVERTED",
        $"Lead '{leadId}' has already been converted to a customer.")
{
}

// 429 — Rate Limited
public class RateLimitException : SFAException
{
    public RateLimitException()
        : base("RATE_LIMITED", "Too many requests. Please retry after the indicated time.") { }
}

// 503 — Infrastructure
public class InfrastructureException : SFAException
{
    protected InfrastructureException(string code, string message)
        : base(code, message) { }
}
public class StorageUnavailableException : InfrastructureException
{
    public StorageUnavailableException()
        : base("SERVICE_UNAVAILABLE", "Storage service is temporarily unavailable.") { }
}
public class DatabaseUnavailableException : InfrastructureException
{
    public DatabaseUnavailableException()
        : base("SERVICE_UNAVAILABLE", "Database is temporarily unavailable.") { }
}
public class LockServiceUnavailableException : InfrastructureException
{
    public LockServiceUnavailableException()
        : base("LOCK_SERVICE_UNAVAILABLE", "Lock service is temporarily unavailable.") { }
}
