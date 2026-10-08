namespace NovaBooks.Application.DTOs.Common;

public sealed class CatalogOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
