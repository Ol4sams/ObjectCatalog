namespace ObjectCatalog.Models;

public class ObjectEntity
{
    public int Id { get; set; }    
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<Category> Categories { get; set; } = [];
}