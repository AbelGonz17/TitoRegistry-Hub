namespace TitoRegistry.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Incluye auditoría básica y soporte nativo para Soft Delete (borrado lógico).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; protected set; } = false;
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Marca la entidad como eliminada lógicamente sin destruirla de la base de datos.
    /// </summary>
    public virtual void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Restaura una entidad previamente eliminada lógicamente.
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
