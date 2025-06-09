namespace ObjectCatalog.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<ObjectEntity> Objects { get; set; } = [];
}