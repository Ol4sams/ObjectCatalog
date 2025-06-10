namespace ObjectCatalog.Models;

public class ObjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public IEnumerable<string> Categories { get; set; } = [];
}
