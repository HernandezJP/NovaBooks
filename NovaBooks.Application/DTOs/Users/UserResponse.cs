namespace NovaBooks.Application.DTOs.Users;

public sealed class UserResponse
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public bool IsLockedOut { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } =
        Array.Empty<string>();
}