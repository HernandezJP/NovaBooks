using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Organization;

public sealed class BranchResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int DepartmentId { get; set; }

    public string Department { get; set; } = string.Empty;

    public int MunicipalityId { get; set; }

    public string Municipality { get; set; } = string.Empty;

    public string? Zone { get; set; }

    public string AddressLine { get; set; } = string.Empty;

    public string? AddressReference { get; set; }

    public bool IsMain { get; set; }

    public bool IsActive { get; set; }

    public int WarehousesCount { get; set; }

    public int CashRegistersCount { get; set; }

    public int EmployeesCount { get; set; }
}

public sealed class SaveBranchRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [BusinessName("nombre", ErrorMessage = "El nombre solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(25, ErrorMessage = "El teléfono no puede superar los 25 caracteres.")]
    [PhoneNumber]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Seleccione el municipio.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el municipio.")]
    public int? MunicipalityId { get; set; }

    [StringLength(2, ErrorMessage = "La zona tiene como máximo 2 dígitos.")]
    [DigitsOnly(ErrorMessage = "La zona solo admite números.")]
    public string? Zone { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 250 caracteres.")]
    [PlainText]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La referencia no puede superar los 500 caracteres.")]
    [PlainText]
    public string? AddressReference { get; set; }

    /// <summary>Marca la sucursal como principal (la anterior deja de serlo).</summary>
    public bool IsMain { get; set; }
}

public sealed class WarehouseResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int BranchId { get; set; }

    public string Branch { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Títulos con existencia o reserva en el almacén.</summary>
    public int BooksWithStock { get; set; }

    public decimal Units { get; set; }
}

public sealed class SaveWarehouseRequest
{
    [Required(ErrorMessage = "Seleccione la sucursal.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sucursal.")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [BusinessName("nombre", ErrorMessage = "El nombre solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Description { get; set; }

    /// <summary>Almacén principal de su sucursal (ventas y recepciones lo proponen primero).</summary>
    public bool IsMain { get; set; }
}

public sealed class CashRegisterAdminResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int BranchId { get; set; }

    public string Branch { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsOpen { get; set; }

    public string? OpenedBy { get; set; }
}

public sealed class SaveCashRegisterRequest
{
    [Required(ErrorMessage = "Seleccione la sucursal.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la sucursal.")]
    public int? BranchId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
    [BusinessName("nombre", ErrorMessage = "El nombre solo admite letras, números, espacios y los signos . , & ' - ( ) /.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Description { get; set; }
}

public sealed class DepartmentOptionResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<CatalogOptionResponse> Municipalities { get; set; } = [];
}

/// <summary>Opciones de los formularios de sucursales, almacenes y cajas.</summary>
public sealed class OrganizationCatalogsResponse
{
    public IReadOnlyList<DepartmentOptionResponse> Departments { get; set; } = [];

    /// <summary>Sucursales activas.</summary>
    public IReadOnlyList<CatalogOptionResponse> Branches { get; set; } = [];
}
