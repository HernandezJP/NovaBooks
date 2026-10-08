namespace NovaBooks.Application.DTOs.Menu;

public sealed class MenuGroupResponse
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public IReadOnlyCollection<MenuItemResponse> Items { get; set; } =
        Array.Empty<MenuItemResponse>();
}

public sealed class MenuItemResponse
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Href { get; set; } = string.Empty;

    /// <summary>
    /// Clave semántica del icono; la Web la traduce a su librería.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    public string? RequiredPermission { get; set; }
}
