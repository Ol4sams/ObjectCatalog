using Microsoft.EntityFrameworkCore;
using ObjectCatalog.Data;
using ObjectCatalog.Models;

namespace ObjectCatalog.Services;

public class ObjectService : IObjectService
{
    private readonly ObjectCatalogDbContext _context;

    public ObjectService(ObjectCatalogDbContext context)
    {
        _context = context;
    }

    public async Task<List<ObjectEntity>> GetAllObjectsAsync()
        => await _context.Objects
                .Include(o => o.Categories)
                .ToListAsync();

    public async Task<List<Category>> GetAllCategoriesAsync()
        => await _context.Categories.ToListAsync();

    public async Task<ObjectEntity> GetObjectByIdAsync(int id)
        => await _context.Objects
            .Include(o => o.Categories)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(List<ObjectEntity> Items, int TotalItems)> GetPagedObjectsAsync(int skip, int take, string? searchQuery = null, int? categoryId = null)
    {
        var query = _context.Objects
            .Include(o => o.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(o => EF.Functions.Like(o.Name, $"%{searchQuery}%"));
            //query = query.Where(o => o.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(o => o.Categories.Any(c => c.Id == categoryId.Value));
        }

        query = query.OrderBy(o => o.Id);
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalItems);
    }

    public async Task AddObjectAsync(ObjectEntity obj)
    {
        obj.CreatedDate = DateTime.UtcNow;
        _context.Objects.Add(obj);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateObjectAsync(int id, ObjectEntity obj)
    {
        var existing = await _context.Objects.FindAsync(id);
        if (existing != null)
        {
            existing.Name = obj.Name;
            existing.Description = obj.Description;
            existing.Price = obj.Price;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteObjectAsync(int id)
    {
        var obj = await _context.Objects.FindAsync(id);
        if (obj != null)
        {
            _context.Objects.Remove(obj);
            await _context.SaveChangesAsync();
        }
    }
}