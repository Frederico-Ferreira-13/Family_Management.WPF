using System;

namespace FamilyManagement.Domain.Common;

public abstract class BaseEntity : ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    public bool IsActive { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }

    protected void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
    
    public virtual void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        Touch();
    }

    public virtual void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        Touch();
    }
}

