namespace ObjectCatalog.Models;

public class ObjectCategory
{
    public int ObjectId { get; set; }
    public ObjectEntity Object { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
}