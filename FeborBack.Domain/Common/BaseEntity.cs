using System.ComponentModel.DataAnnotations;

namespace FeborBack.Domain.Common;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void SetCreatedBy(int userId)
    {
        CreatedBy = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetUpdatedBy(int userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(int userId)
    {
        IsDeleted = true;
        DeletedBy = userId;
        DeletedAt = DateTime.UtcNow;
    }

    public void Activate(int userId)
    {
        IsActive = true;
        SetUpdatedBy(userId);
    }

    public void Deactivate(int userId)
    {
        IsActive = false;
        SetUpdatedBy(userId);
    }
}