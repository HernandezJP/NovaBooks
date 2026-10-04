using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Users;

public sealed class AssignUserRolesRequest
{
    [Required(ErrorMessage = "Debe enviar la lista de roles.")]
    public List<string> Roles { get; set; } = [];
}