using Bonzer.Application.Security;
using Bonzer.Domain.Common;
using Bonzer.Infrastructure.Persistence.Encryption;

namespace Bonzer.UnitTests.Security;

public class SecurityTests
{
    [Fact]
    public void Encryptor_round_trips_and_uses_a_random_nonce()
    {
        var encryptor = new AesGcmFieldEncryptor(TestDb.Key);

        var first = encryptor.Encrypt("123412341234");
        var second = encryptor.Encrypt("123412341234");

        Assert.StartsWith("v1:", first);
        Assert.NotEqual(first, second);
        Assert.Equal("123412341234", encryptor.Decrypt(first));
    }

    [Fact]
    public void Encryptor_passes_through_legacy_plaintext()
    {
        var encryptor = new AesGcmFieldEncryptor(TestDb.Key);
        Assert.Equal("plain-value", encryptor.Decrypt("plain-value"));
    }

    [Fact]
    public void UuidV7_sets_version_and_is_time_ordered()
    {
        var first = UuidV7.NewGuid();
        Thread.Sleep(2);
        var second = UuidV7.NewGuid();

        Assert.Equal('7', first.ToString()[14]);
        Assert.True(string.CompareOrdinal(first.ToString(), second.ToString()) < 0);
    }

    [Fact]
    public void Permission_codes_are_unique()
    {
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
    }

    [Fact]
    public void Default_roles_only_reference_known_permissions()
    {
        var known = Permissions.All.ToHashSet();
        foreach (var (role, permissions) in AppRoles.DefaultPermissions)
            Assert.All(permissions, p => Assert.True(known.Contains(p), $"{role} references unknown permission {p}"));
    }

    [Fact]
    public void Parents_cannot_browse_the_student_register()
    {
        var parent = AppRoles.DefaultPermissions[AppRoles.Parent];
        Assert.DoesNotContain(Permissions.Students.View, parent);
        Assert.Contains(Permissions.Portal.Access, parent);
    }
}
