using System.ComponentModel.DataAnnotations;

namespace NovaBooks.Application.DTOs.Roles;

public sealed class AssignRolePermissionsRequest
{
    [Required(ErrorMessage = "Debe enviar la lista de permisos.")]
    public List<string> Permissions { get; set; } = [];
}