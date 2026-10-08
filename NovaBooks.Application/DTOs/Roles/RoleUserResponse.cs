namespace NovaBooks.Application.DTOs.Roles;

public sealed class RoleUserResponse
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
