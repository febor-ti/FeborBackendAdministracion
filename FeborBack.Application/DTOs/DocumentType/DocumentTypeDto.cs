using System.ComponentModel.DataAnnotations;

namespace FeborBack.Application.DTOs.DocumentType;

public class DocumentTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool ShowInPortal { get; set; }
    public string? ButtonColor { get; set; }
    public string? ButtonTitle { get; set; }
    public string? ButtonSubtitle { get; set; }
    public bool IsActive { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public List<DocumentTypeDto> Children { get; set; } = [];
}

public class CreateDocumentTypeDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool ShowInPortal { get; set; }
    public string? ButtonColor { get; set; }
    [StringLength(150)]
    public string? ButtonTitle { get; set; }
    [StringLength(200)]
    public string? ButtonSubtitle { get; set; }
}

public class UpdateDocumentTypeDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool ShowInPortal { get; set; }
    public string? ButtonColor { get; set; }
    [StringLength(150)]
    public string? ButtonTitle { get; set; }
    [StringLength(200)]
    public string? ButtonSubtitle { get; set; }
}
