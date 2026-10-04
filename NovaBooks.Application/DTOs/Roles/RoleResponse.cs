namespace NovaBooks.Application.DTOs.Roles;

public sealed class RoleResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int UsersCount { get; set; }

    public IReadOnlyCollection<string> Permissions { get; set; } =
        Array.Empty<string>();
}