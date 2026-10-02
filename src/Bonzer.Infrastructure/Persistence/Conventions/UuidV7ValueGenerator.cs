using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Bonzer.Infrastructure.Persistence.Conventions;

/// <summary>Generates time-ordered UUID v7 keys on the client, so ids are known before SaveChanges.</summary>
public sealed class UuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => UuidV7.NewGuid();
}
