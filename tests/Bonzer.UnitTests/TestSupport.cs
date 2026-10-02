using Bonzer.Application.Common.Interfaces;
using Bonzer.Domain.Enums;
using Bonzer.Infrastructure.Persistence;
using Bonzer.Infrastructure.Persistence.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bonzer.UnitTests;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public bool IsAuthenticated { get; init; } = true;
    public bool IsSystem { get; init; }
    public Guid? UserId { get; init; } = Guid.NewGuid();
    public string? UserName { get; init; } = "tester";
    public Guid? TenantId { get; init; }
    public UserType? UserType { get; init; } = Domain.Enums.UserType.Staff;
    public IReadOnlyList<Guid> BranchIds { get; init; } = [];
    public IReadOnlyList<string> Roles { get; init; } = [];
    public Guid? StaffId { get; init; }
    public Guid? GuardianId { get; init; }
    public Guid? StudentId { get; init; }
    public bool IsPlatformAdmin { get; init; }
    public bool HasTenantWideAccess { get; init; }
    public string? IpAddress => "127.0.0.1";
    public string? CorrelationId => "test";
    public bool IsInRole(string role) => Roles.Contains(role);
}

internal static class TestDb
{
    public static readonly string Key = Convert.ToBase64String(new byte[32]);

    /// <summary>Npgsql context that never opens a connection: enough for model checks and ToQueryString().</summary>
    public static BonzerDbContext Create(ICurrentUser user) =>
        new(new DbContextOptionsBuilder<BonzerDbContext>()
                .UseNpgsql("Host=localhost;Database=unused")
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options,
            user, new AesGcmFieldEncryptor(Key));
}
