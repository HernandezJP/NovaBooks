namespace TikalLibros.Application.DTOs.Common;

/// <summary>Activa o desactiva (eliminación lógica) un registro.</summary>
public sealed class ChangeActiveStatusRequest
{
    public bool IsActive { get; set; }
}
