using FeborBack.Application.DTOs.Forms;

namespace FeborBack.Application.Services.Forms;

public interface IFormService
{
    Task<IEnumerable<FormPageDto>> GetAllAsync();
    Task<FormPageDto> CreateAsync(CreateFormPageDto dto, Stream fileStream, string fileName, int createdBy);
    Task<FormPageDto> UpdateAsync(int id, UpdateFormPageDto dto, Stream? fileStream, string? fileName, int updatedBy);
    Task<FormPageDto> ToggleActiveAsync(int id, int updatedBy);
    Task DeleteAsync(int id, int deletedBy);
}
