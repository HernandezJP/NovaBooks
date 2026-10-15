namespace TikalLibros.Application.DTOs.Users;

/// <summary>Vincula el usuario con un empleado; null lo desvincula.</summary>
public sealed class AssignUserEmployeeRequest
{
    public int? EmployeeId { get; set; }
}
