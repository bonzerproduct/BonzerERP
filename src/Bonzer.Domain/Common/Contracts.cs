namespace Bonzer.Domain.Common;

/// <summary>Who created / last changed the row and when (UTC). Filled automatically on save.</summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>Rows are never physically deleted; a global query filter hides deleted rows.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

/// <summary>Row belongs to one customer (tenant). A global query filter isolates tenants.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>Row belongs to one campus/branch of a tenant.</summary>
public interface IBranchEntity
{
    Guid BranchId { get; set; }
}

/// <summary>Optimistic concurrency token (mapped to PostgreSQL <c>xmin</c>).</summary>
public interface IHasRowVersion
{
    uint RowVersion { get; set; }
}

/// <summary>Tenant-defined extra fields stored as JSON (jsonb) so schools can add fields without migrations.</summary>
public interface IHasCustomFields
{
    string? CustomFields { get; set; }
}
