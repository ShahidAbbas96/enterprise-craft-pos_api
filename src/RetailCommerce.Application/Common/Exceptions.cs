namespace RetailCommerce.Application.Common;

/// <summary>Entity of the given type/id was not found. The Api layer maps this to HTTP 404.</summary>
public class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

/// <summary>A uniqueness or state conflict (duplicate SKU/phone, invalid state transition).
/// The Api layer maps this to HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Field-level validation failure. The Api layer maps this to HTTP 400 with the
/// Errors dictionary serialized as ProblemDetails-style validation errors.</summary>
public class ValidationAppException(IDictionary<string, string[]> errors) : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>Credentials were rejected or a token is invalid/expired. The Api layer maps this to HTTP 401.</summary>
public class AuthenticationFailedException(string message) : Exception(message);

/// <summary>The caller is authenticated but not allowed to act on the given resource — most
/// commonly a terminal-scoped POS token naming a WarehouseId that isn't its own. The Api layer
/// maps this to HTTP 403.</summary>
public class ForbiddenException(string message) : Exception(message);
