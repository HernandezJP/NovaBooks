namespace NovaBooks.Application.DTOs.Roles;

public sealed class RoleResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Rol protegido (Administrador): no se renombra, no se
    /// desactiva y sus permisos no se modifican.
    /// </summary>
    public bool IsProtected { get; set; }

    public int UsersCount { get; set; }

    public IReadOnlyCollection<string> Permissions { get; set; } =
        Array.Empty<string>();
}