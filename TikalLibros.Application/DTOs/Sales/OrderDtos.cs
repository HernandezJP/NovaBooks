using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Sales;

public sealed class OrderLineRequest
{
    [Required(ErrorMessage = "Seleccione el libro.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el libro.")]
    public int? BookId { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(1, 10000, ErrorMessage = "La cantidad debe ser un número entero entre 1 y 10,000.")]
    public int? Quantity { get; set; }

    /// <summary>Porcentaje sobre el precio de lista; requiere Ventas.AplicarDescuento si es mayor que 0.</summary>
    [Range(typeof(decimal), "0", "100", ErrorMessage = "El descuento debe estar entre 0 y 100 %.")]
    [MaxDecimals(2, "El descuento admite como máximo 2 decimales.")]
    public decimal DiscountPercent { get; set; }
}

public sealed class SaveOrderRequest : IValidatableObject
{
    public const int MaxLines = 200;

    [Required(ErrorMessage = "Seleccione el cliente.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el cliente.")]
    public int? CustomerId { get; set; }

    /// <summary>CONTADO (predeterminado) o CREDITO.</summary>
    [RegularExpression("^(CONTADO|CREDITO)$", ErrorMessage = "La condición de pago debe ser contado o crédito.")]
    public string SaleType { get; set; } = Sales.SaleType.Cash;

    /// <summary>Si no se indica, la lista predeterminada.</summary>
    public int? PriceListId { get; set; }

    /// <summary>Si no se indica, el almacén principal.</summary>
    public int? WarehouseId { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    [UniqueBy(nameof(OrderLineRequest.BookId), "Cada libro solo puede aparecer una vez en el pedido.")]
    public List<OrderLineRequest> Lines { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines.Count == 0)
        {
            yield return new ValidationResult("Agregue al menos un libro al pedido.", [nameof(Lines)]);
        }
        else if (Lines.Count > MaxLines)
        {
            yield return new ValidationResult($"El pedido admite como máximo {MaxLines} libros.", [nameof(Lines)]);
        }

        foreach (ValidationResult result in NestedValidation.Validate(Lines, nameof(Lines)))
        {
            yield return result;
        }
    }
}

public sealed class ApproveOrderRequest
{
    [StringLength(500, ErrorMessage = "El comentario no puede superar los 500 caracteres.")]
    [PlainText]
    public string? Comment { get; set; }
}

/// <summary>Rechazo o cancelación: el motivo es obligatorio.</summary>
public sealed class OrderReasonRequest
{
    [Required(ErrorMessage = "Indique el motivo.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}

public sealed class OrderListItemResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SaleType { get; set; } = string.Empty;

    public decimal Units { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    public DateTime? ReservationExpiresAt { get; set; }

    public string Employee { get; set; } = string.Empty;
}

public sealed class OrderLineResponse
{
    public int Id { get; set; }

    public int LineNumber { get; set; }

    public int BookId { get; set; }

    public string BookCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Reserved { get; set; }

    public decimal Invoiced { get; set; }

    /// <summary>Precio de lista con IVA.</summary>
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercent { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxPercent { get; set; }

    /// <summary>IVA incluido en el total de la línea.</summary>
    public decimal TaxAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    /// <summary>Existencia libre ahora (disponible menos reservada por otros pedidos).</summary>
    public decimal Available { get; set; }
}

public sealed class OrderResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerCode { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string? CustomerNit { get; set; }

    public decimal CustomerCreditLimit { get; set; }

    /// <summary>Saldo pendiente de las ventas al crédito del cliente.</summary>
    public decimal CustomerCreditUsed { get; set; }

    public int WarehouseId { get; set; }

    public string Warehouse { get; set; } = string.Empty;

    public string StatusCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SaleTypeCode { get; set; } = string.Empty;

    public string SaleType { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public DateTime? ReservationExpiresAt { get; set; }

    public string Employee { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Tax { get; set; }

    public decimal Total { get; set; }

    /// <summary>Por qué necesita (o necesitó) la aprobación de un supervisor.</summary>
    public IReadOnlyCollection<string> ApprovalReasons { get; set; } = [];

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? ApprovalComment { get; set; }

    public string? CancellationReason { get; set; }

    public IReadOnlyCollection<OrderLineResponse> Lines { get; set; } = [];

    public bool CanEdit { get; set; }

    public bool CanConfirm { get; set; }

    public bool CanApprove { get; set; }

    public bool CanCancel { get; set; }

    public bool CanInvoice { get; set; }
}

/// <summary>Búsqueda avanzada de pedidos: todos los filtros se combinan.</summary>
public sealed class OrderQuery
{
    /// <summary>Número de pedido, código, nombre o NIT del cliente.</summary>
    public string? Search { get; set; }

    public string? Status { get; set; }

    public int? CustomerId { get; set; }

    public string? SaleType { get; set; }

    public int? EmployeeId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public decimal? MinTotal { get; set; }

    public decimal? MaxTotal { get; set; }

    /// <summary>Libro incluido en el pedido.</summary>
    public int? BookId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}

public sealed class SalesBookOptionResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Isbn13 { get; set; }

    /// <summary>Precio con IVA en la lista solicitada; null si el libro no tiene precio en ella.</summary>
    public decimal? Price { get; set; }

    public decimal TaxPercent { get; set; }

    /// <summary>Existencia libre en el almacén solicitado.</summary>
    public decimal Available { get; set; }
}

public sealed class OrderCatalogsResponse
{
    public IReadOnlyCollection<CatalogOptionResponse> PriceLists { get; set; } = [];

    public int? DefaultPriceListId { get; set; }

    public IReadOnlyCollection<CatalogOptionResponse> Warehouses { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> SaleTypes { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> Statuses { get; set; } = [];

    /// <summary>Descuento por línea que no requiere aprobación.</summary>
    public decimal DiscountWithoutApproval { get; set; }

    public decimal MaximumDiscount { get; set; }

    public int ReservationDays { get; set; }
}
