namespace Bonzer.Application.Common.Exceptions;

/// <summary>Mapped to HTTP 404 by the global exception handler.</summary>
public sealed class NotFoundException(string entity, object key)
    : Exception($"{entity} '{key}' was not found.");

/// <summary>Mapped to HTTP 403.</summary>
public sealed class ForbiddenAccessException(string message = "You do not have permission to perform this action.")
    : Exception(message);

/// <summary>Mapped to HTTP 401 (bad credentials, expired or reused refresh token).</summary>
public sealed class UnauthorizedException(string message = "Invalid credentials.") : Exception(message);

/// <summary>Mapped to HTTP 409 (duplicate admission number, concurrent edit...).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Mapped to HTTP 422 when a business rule is violated (section full, attendance locked...).</summary>
public sealed class BusinessRuleException(string message) : Exception(message);
