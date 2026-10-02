namespace Bonzer.Application.Common.Exceptions;

/// <summary>
/// Stable error codes returned as ProblemDetails "code". Clients switch on these, so never rename one;
/// add a new code instead.
/// </summary>
public static class ErrorCodes
{
    // 400
    public const string ValidationFailed = "validation_failed";
    public const string BadRequest = "bad_request";

    // 401
    public const string Unauthenticated = "unauthenticated";
    public const string InvalidCredentials = "invalid_credentials";
    public const string AmbiguousLogin = "ambiguous_login";
    public const string AccountLocked = "account_locked";
    public const string AccountDisabled = "account_disabled";
    public const string SubscriptionInactive = "subscription_inactive";
    public const string TokenExpired = "token_expired";
    public const string TokenInvalid = "token_invalid";
    public const string SessionRevoked = "session_revoked";
    public const string SessionExpired = "session_expired";

    // 403
    public const string Forbidden = "forbidden";
    public const string PasswordChangeRequired = "password_change_required";
    public const string CrossTenantAccess = "cross_tenant_access";

    // 404 / 405
    public const string NotFound = "not_found";
    public const string MethodNotAllowed = "method_not_allowed";

    // 409
    public const string Conflict = "conflict";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string DuplicateRecord = "duplicate_record";
    public const string RecordInUse = "record_in_use";

    // 413 / 415
    public const string PayloadTooLarge = "payload_too_large";
    public const string UnsupportedMediaType = "unsupported_media_type";

    // 422
    public const string BusinessRule = "business_rule_violation";
    public const string ConstraintViolation = "constraint_violation";
    public const string InvalidResetToken = "invalid_reset_token";

    // 429
    public const string RateLimited = "rate_limited";

    // 499 (client went away; never sent, only logged)
    public const string ClientClosedRequest = "client_closed_request";

    // 5xx
    public const string InternalError = "internal_error";
    public const string ServiceUnavailable = "service_unavailable";
    public const string DatabaseUnavailable = "database_unavailable";
    public const string TransientFailure = "transient_failure";
    public const string MessageDeliveryFailed = "message_delivery_failed";
    public const string Timeout = "timeout";

    /// <summary>Default code for a status that was produced without an exception (e.g. an empty 404).</summary>
    public static string ForStatus(int status) => status switch
    {
        400 => BadRequest,
        401 => Unauthenticated,
        403 => Forbidden,
        404 => NotFound,
        405 => MethodNotAllowed,
        409 => Conflict,
        413 => PayloadTooLarge,
        415 => UnsupportedMediaType,
        422 => BusinessRule,
        429 => RateLimited,
        503 => ServiceUnavailable,
        504 => Timeout,
        >= 500 => InternalError,
        _ => BadRequest
    };
}
