using Bonzer.Infrastructure.Persistence.Encryption;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bonzer.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef migrations add". No database connection is needed to create a migration.
/// Override the connection with the BONZER_DB environment variable to run "dotnet ef database update".
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BonzerDbContext>
{
    public BonzerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BONZER_DB")
                               ?? "Host=localhost;Port=5432;Database=bonzer_erp;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<BonzerDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
            .Options;

        return new BonzerDbContext(options, new DesignTimeUser(), new AesGcmFieldEncryptor(Convert.ToBase64String(new byte[32])));
    }

    private sealed class DesignTimeUser : ICurrentUser
    {
        public bool IsAuthenticated => false;
        public bool IsSystem => true;
        public Guid? UserId => null;
        public string? UserName => "design-time";
        public Guid? TenantId => null;
        public UserType? UserType => null;
        public IReadOnlyList<Guid> BranchIds => [];
        public IReadOnlyList<string> Roles => [];
        public Guid? StaffId => null;
        public Guid? GuardianId => null;
        public Guid? StudentId => null;
        public bool IsPlatformAdmin => false;
        public bool HasTenantWideAccess => false;
        public string? IpAddress => null;
        public string? CorrelationId => null;
        public bool IsInRole(string role) => false;
    }
}
