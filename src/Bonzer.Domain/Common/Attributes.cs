namespace Bonzer.Domain.Common;

/// <summary>Column is encrypted at rest (AES-GCM) and masked in audit logs. Use for Aadhaar / national IDs.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EncryptedAttribute : Attribute;

/// <summary>Entity or property is excluded from the automatic audit trail (high-volume or non-business data).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute;
