using ObjectCatalog.Models;

namespace ObjectCatalog.Services;

public interface IObjectService
{
    Task<List<ObjectEntity>> GetAllObjectsAsync();
    Task<List<Category>> GetAllCategoriesAsync();
    Task<ObjectEntity> GetObjectByIdAsync(int id);
    Task AddObjectAsync(ObjectEntity obj, int[] categoryIds);
    Task<ObjectEntity?> UpdateObjectAsync(int id, ObjectEntity obj, int[] categoryIds);
    Task DeleteObjectAsync(int id);
    Task<(List<ObjectEntity> Items, int TotalItems)> GetPagedObjectsAsync(int skip, int take, string? searchQuery = null, int? categoryId = null);
}