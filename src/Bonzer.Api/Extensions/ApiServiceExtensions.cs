using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Diagnostics;
using Bonzer.Api.Authorization;
using Bonzer.Application.Common.Exceptions;
using Bonzer.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Bonzer.Api;

public static class RateLimitPolicies
{
    public const string Auth = "auth";
}

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Every error body (exceptions, model validation, empty 401/403/404/405, 429) gets the same shape:
        // type, title, status, detail, instance + code, correlationId, traceId.
        services.AddProblemDetails(o => o.CustomizeProblemDetails = context =>
        {
            var problem = context.ProblemDetails;
            var http = context.HttpContext;
            problem.Instance ??= http.Request.Path;
            problem.Extensions["correlationId"] = http.TraceIdentifier;
            problem.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? http.TraceIdentifier;
            if (!problem.Extensions.ContainsKey("code"))
                problem.Extensions["code"] = problem is HttpValidationProblemDetails
                    ? ErrorCodes.ValidationFailed
                    : ErrorCodes.ForStatus(problem.Status ?? http.Response.StatusCode);
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Permission-based authorization; every endpoint requires an authenticated user by default.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Brute-force protection on login / refresh / password recovery: N attempts per minute per IP.
        var authPermitLimit = configuration.GetValue("RateLimiting:AuthPermitLimit", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? (int)Math.Ceiling(wait.TotalSeconds) : 60;
                http.Response.Headers.RetryAfter = retryAfter.ToString();
                http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Bonzer.Api.RateLimiting")
                    .LogWarning("Rate limit exceeded for {ClientIp} on {Path}", http.Connection.RemoteIpAddress?.ToString(), http.Request.Path.Value);

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = $"Too many attempts. Try again in {retryAfter} seconds."
                };
                await http.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
            };
            options.AddPolicy(RateLimitPolicies.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermitLimit, Window = TimeSpan.FromMinutes(1) }));
        });

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        var health = services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!, name: "postgres", tags: ["ready"]);
        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
            health.AddRedis(redis, name: "redis", tags: ["ready"]);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "Bonzer ERP API", Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the accessToken from /api/v1/auth/login"
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
            o.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));
            var xml = Path.Combine(AppContext.BaseDirectory, "Bonzer.Api.xml");
            if (File.Exists(xml)) o.IncludeXmlComments(xml);
        });

        return services;
    }
}
