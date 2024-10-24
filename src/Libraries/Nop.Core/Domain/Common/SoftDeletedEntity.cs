namespace Nop.Core.Domain.Common;

/// <summary>
/// Represents a soft-deleted (without actually deleting from storage) entity
/// </summary>
public abstract partial class SoftDeletedEntity : BaseEntity
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity has been deleted
    /// </summary>
    public bool Deleted { get; set; }
}