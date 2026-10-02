namespace Bonzer.Application.Common.Exceptions;

/// <summary>
/// Base of every expected (business / access) failure. <see cref="Code"/> is a stable, machine-readable
/// value returned to clients as ProblemDetails "code" (see <see cref="ErrorCodes"/>), so front-ends can
/// react without parsing messages. The message is safe to show to the user.
/// </summary>
public abstract class AppException(string code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string Code { get; } = code;
}

/// <summary>Mapped to HTTP 404 by the global exception handler.</summary>
public sealed class NotFoundException(string entity, object key, string code = ErrorCodes.NotFound)
    : AppException(code, $"{entity} '{key}' was not found.")
{
    public string Entity { get; } = entity;
    public object Key { get; } = key;
}

/// <summary>Mapped to HTTP 403.</summary>
public sealed class ForbiddenAccessException(
    string message = "You do not have permission to perform this action.", string code = ErrorCodes.Forbidden)
    : AppException(code, message);

/// <summary>Mapped to HTTP 401 (bad credentials, expired or reused refresh token).</summary>
public sealed class UnauthorizedException(string message = "Invalid credentials.", string code = ErrorCodes.Unauthenticated)
    : AppException(code, message);

/// <summary>Mapped to HTTP 409 (duplicate admission number, concurrent edit...).</summary>
public sealed class ConflictException(string message, string code = ErrorCodes.Conflict) : AppException(code, message);

/// <summary>Mapped to HTTP 422 when a business rule is violated (section full, attendance locked...).</summary>
public sealed class BusinessRuleException(string message, string code = ErrorCodes.BusinessRule) : AppException(code, message);

/// <summary>
/// Mapped to HTTP 503: a dependency (SMS / email / payment gateway) failed. The message is user safe;
/// provider details belong in the inner exception, which is logged but never returned.
/// </summary>
public class ExternalServiceException(string service, string message, Exception? innerException = null,
    string code = ErrorCodes.ServiceUnavailable)
    : AppException(code, message, innerException)
{
    public string Service { get; } = service;
}

/// <summary>An email or SMS could not be handed to the provider.</summary>
public sealed class MessageDeliveryException(string channel, Exception innerException)
    : ExternalServiceException(channel, $"The {channel} could not be sent. Please try again later.", innerException,
        ErrorCodes.MessageDeliveryFailed);
