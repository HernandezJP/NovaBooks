using NovaBooks.Application.DTOs.Common;

namespace NovaBooks.Application.DTOs.Customers;

public sealed class CustomerListItemResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string PersonType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Documento principal con su tipo, por ejemplo "DPI 1234567890101".
    /// </summary>
    public string? Identification { get; set; }

    public string? Nit { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public decimal CreditLimit { get; set; }

    public int CreditDays { get; set; }

    public bool IsActive { get; set; }
}

public sealed class CustomerResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string PersonType { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public string? SecondLastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? LegalName { get; set; }

    public string? TradeName { get; set; }

    public int? IdentificationTypeId { get; set; }

    public string? IdentificationTypeCode { get; set; }

    public string? IdentificationNumber { get; set; }

    public string? Nit { get; set; }

    public string? Email { get; set; }

    public int? PhoneTypeId { get; set; }

    public string? Phone { get; set; }

    public string? PhoneExtension { get; set; }

    public decimal CreditLimit { get; set; }

    public int CreditDays { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }
}

public sealed class ChangeCustomerStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class CustomerQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>NATURAL o JURIDICA.</summary>
    public string? PersonType { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class CustomerCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> PersonTypes { get; set; } =
        Array.Empty<CatalogOptionResponse>();

    /// <summary>Documentos de identificación (sin NIT).</summary>
    public IReadOnlyCollection<IdentificationTypeResponse> IdentificationTypes { get; set; } =
        Array.Empty<IdentificationTypeResponse>();

    public IReadOnlyCollection<CatalogOptionResponse> PhoneTypes { get; set; } =
        Array.Empty<CatalogOptionResponse>();
}

public sealed class IdentificationTypeResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }
}
