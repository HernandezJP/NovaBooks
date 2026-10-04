namespace NovaBooks.Application.DTOs.Permissions;

public sealed class PermissionGroupResponse
{
    public string Module { get; set; } = string.Empty;

    public IReadOnlyCollection<PermissionResponse> Permissions
    {
        get;
        set;
    } = Array.Empty<PermissionResponse>();
}