namespace NovaBooks.Application.DTOs.Roles;

public sealed class RoleOptionResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
