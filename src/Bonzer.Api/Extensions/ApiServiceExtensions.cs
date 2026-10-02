using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Bonzer.Api.Authorization;
using Bonzer.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
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

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Permission-based authorization; every endpoint requires an authenticated user by default.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Brute-force protection on login / refresh: 10 attempts per minute per IP.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicies.Auth, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
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
