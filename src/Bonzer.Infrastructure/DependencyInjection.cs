using System.Text;
using Bonzer.Application.Features.Auth;
using Bonzer.Application.Features.Roles;
using Bonzer.Application.Security;
using Bonzer.Infrastructure.Caching;
using Bonzer.Infrastructure.Persistence.Encryption;
using Bonzer.Infrastructure.Persistence.Interceptors;
using Bonzer.Infrastructure.Persistence.Seed;
using Bonzer.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Bonzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        AddPersistence(services, configuration, environment);
        AddIdentityAndJwt(services, configuration);
        AddCaching(services, configuration);

        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddScoped<DbInitializer>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<EncryptionOptions>()
            .Bind(configuration.GetSection(EncryptionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IFieldEncryptor, AesGcmFieldEncryptor>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<AuditTrailInterceptor>();

        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("ConnectionStrings:Database is not configured.");

        services.AddDbContext<BonzerDbContext>((sp, options) =>
        {
            // No EnableRetryOnFailure: services use explicit transactions, which a retrying strategy rejects.
            options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(BonzerDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");
                })
                .UseSnakeCaseNamingConvention()
                // Order matters: stamp / soft-delete first, then write the audit trail.
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>(), sp.GetRequiredService<AuditTrailInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

            if (environment.IsDevelopment())
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<BonzerDbContext>());
    }

    private static void AddIdentityAndJwt(IServiceCollection services, IConfiguration configuration)
    {
        // Registered before AddRoles so Identity's TryAdd keeps this one instead of the global-unique RoleValidator.
        services.AddScoped<IRoleValidator<ApplicationRole>, TenantRoleValidator>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false; // siblings' parents and staff may share emails
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<BonzerDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddDataProtection().SetApplicationName("bonzer-erp");

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Jwt section is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep "sub", "role", "tenant_id" as issued
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = AppClaimTypes.Role
                };
            });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<RoleResolver>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleService, RoleService>();
    }

    private static void AddCaching(IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddDistributedMemoryCache(); // single-instance / local development only
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "bonzer:";
            });
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();
    }
}
