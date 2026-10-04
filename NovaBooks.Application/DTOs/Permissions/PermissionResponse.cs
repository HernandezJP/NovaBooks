namespace NovaBooks.Application.DTOs.Permissions;

public sealed class PermissionResponse
{
    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}