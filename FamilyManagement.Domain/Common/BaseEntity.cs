using System;

namespace FamilyManagement.Domain.Common;

// Criei a BaseEntity para reunir as propriedades e os comportamentos
// comuns às entidades, evitando repeti-los em cada classe.

// Coloquei aqui o identificador, as datas de criação e alteração
// e o estado ativo/inativo.

// Defini a classe como abstrata porque não pretendo criar uma
// BaseEntity diretamente. Quero que seja a base de entidades
// concretas, como Account, Goal e User.
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

