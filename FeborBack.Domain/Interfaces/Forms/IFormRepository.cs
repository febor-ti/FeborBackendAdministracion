using FeborBack.Domain.Entities.Forms;

namespace FeborBack.Domain.Interfaces.Forms;

public interface IFormRepository
{
    Task<IEnumerable<FormPage>> GetAllAsync();
    Task<FormPage?> GetByIdAsync(int id);
    Task<FormPage?> GetBySlugAsync(string slug);
    Task<FormPage> CreateAsync(FormPage form);
    Task<FormPage> UpdateAsync(FormPage form);
    Task DeleteAsync(FormPage form);
    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);
}
