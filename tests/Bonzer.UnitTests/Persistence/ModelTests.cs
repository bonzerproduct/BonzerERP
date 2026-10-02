using Bonzer.Application.Features.Students;
using Bonzer.Domain.Common;
using Bonzer.Domain.Entities.Students;
using Bonzer.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bonzer.UnitTests.Persistence;

public class ModelTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    [Fact]
    public void Every_tenant_entity_has_a_query_filter_and_tenant_foreign_key()
    {
        using var db = TestDb.Create(new FakeCurrentUser { TenantId = TenantA });

        var tenantTypes = db.Model.GetEntityTypes()
            .Where(e => !e.IsOwned() && typeof(ITenantEntity).IsAssignableFrom(e.ClrType)).ToList();

        Assert.NotEmpty(tenantTypes);
        Assert.All(tenantTypes, e =>
        {
            Assert.NotNull(e.GetQueryFilter());
            Assert.Contains(e.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == nameof(ITenantEntity.TenantId)));
        });
    }

    [Fact]
    public void Business_tables_never_cascade_delete()
    {
        using var db = TestDb.Create(new FakeCurrentUser { TenantId = TenantA });

        var cascading = db.Model.GetEntityTypes()
            .Where(e => e.ClrType.Assembly == typeof(BaseEntity).Assembly)
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(fk => $"{fk.DeclaringEntityType.DisplayName()} -> {fk.PrincipalEntityType.DisplayName()}")
            .ToList();

        Assert.Empty(cascading);
    }

    [Fact]
    public void Enums_are_stored_as_strings()
    {
        using var db = TestDb.Create(new FakeCurrentUser { TenantId = TenantA });
        var status = db.Model.FindEntityType(typeof(Student))!.FindProperty(nameof(Student.Status))!;

        Assert.Equal(typeof(string), status.GetProviderClrType());
    }

    [Fact]
    public void Tenant_filter_is_evaluated_per_context_not_cached_from_the_first_one()
    {
        using var dbA = TestDb.Create(new FakeCurrentUser { TenantId = TenantA });
        using var dbB = TestDb.Create(new FakeCurrentUser { TenantId = TenantB });

        var sqlA = dbA.Students.ToQueryString();
        var sqlB = dbB.Students.ToQueryString();

        Assert.Contains("tenant_id", sqlA);
        Assert.Contains("is_deleted", sqlA);
        Assert.Contains(TenantA.ToString(), sqlA);
        Assert.Contains(TenantB.ToString(), sqlB);
        Assert.DoesNotContain(TenantA.ToString(), sqlB);
    }

    [Fact]
    public void Student_list_projection_translates_to_sql()
    {
        using var db = TestDb.Create(new FakeCurrentUser { TenantId = TenantA });

        var sql = StudentService.ProjectToListItem(db.Students.Where(s => s.Status == StudentStatus.Active)).ToQueryString();

        Assert.Contains("student_enrollments", sql);
        Assert.Contains("guardians", sql);
    }
}
