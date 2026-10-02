namespace Bonzer.Domain.Common;

public abstract class BaseEntity
{
    /// <summary>UUID v7 (time ordered), generated client-side on insert. See <see cref="UuidV7"/>.</summary>
    public Guid Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity, IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>Base for almost every business table: tenant isolated, audited and soft deletable.</summary>
public abstract class TenantEntity : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

/// <summary>Tenant entity that is also scoped to a single branch/campus.</summary>
public abstract class BranchEntity : TenantEntity, IBranchEntity
{
    public Guid BranchId { get; set; }
}
