namespace ObjectCatalog.Models;

public class PagedObjectResponseDto
{
    public IEnumerable<ObjectDto> Items { get; set; } = [];
    public int TotalItems { get; set; }
}