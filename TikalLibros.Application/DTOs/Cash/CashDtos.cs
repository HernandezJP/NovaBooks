using System.ComponentModel.DataAnnotations;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.Validation;

namespace TikalLibros.Application.DTOs.Cash;

/// <summary>Códigos de tipo de movimiento de caja.</summary>
public static class CashMovementTypes
{
    /// <summary>Lo genera el sistema al cobrar una venta; no se registra a mano.</summary>
    public const string Sale = "VENTA";

    public const string ManualIncome = "INGRESO_MANUAL";
    public const string Refund = "REEMBOLSO";
    public const string Withdrawal = "RETIRO";
    public const string Expense = "GASTO";
    public const string PositiveAdjustment = "AJUSTE_POSITIVO";
    public const string NegativeAdjustment = "AJUSTE_NEGATIVO";
}

public sealed class CashRegisterResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Branch { get; set; } = string.Empty;

    public int? OpenSessionId { get; set; }

    public string? OpenedBy { get; set; }

    public DateTime? OpenedAt { get; set; }
}

public sealed class CashMovementTypeResponse
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>1 ingreso, -1 egreso.</summary>
    public int Nature { get; set; }
}

public sealed class CashCatalogsResponse
{
    public IReadOnlyCollection<CashRegisterResponse> CashRegisters { get; set; } = [];

    /// <summary>Solo los que se registran a mano.</summary>
    public IReadOnlyCollection<CashMovementTypeResponse> MovementTypes { get; set; } = [];

    public IReadOnlyCollection<CatalogOptionResponse> PaymentMethods { get; set; } = [];
}

public sealed class OpenCashSessionRequest
{
    [Required(ErrorMessage = "Seleccione la caja.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione la caja.")]
    public int? CashRegisterId { get; set; }

    [Required(ErrorMessage = "Indique el monto inicial (puede ser 0).")]
    [Range(typeof(decimal), "0", "100000", ErrorMessage = "El monto inicial debe estar entre 0 y 100,000.")]
    [MaxDecimals(2, "El monto inicial admite como máximo 2 decimales.")]
    public decimal? InitialAmount { get; set; }

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }
}

public sealed class CashMovementRequest
{
    [Required(ErrorMessage = "Seleccione el tipo de movimiento.")]
    [StringLength(30, ErrorMessage = "Seleccione el tipo de movimiento.")]
    [RegularExpression("^[A-Z_]+$", ErrorMessage = "Seleccione el tipo de movimiento.")]
    public string TypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indique el monto.")]
    [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "El monto debe estar entre 0.01 y 100,000.")]
    [MaxDecimals(2, "El monto admite como máximo 2 decimales.")]
    public decimal? Amount { get; set; }

    [Required(ErrorMessage = "Indique el concepto.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "El concepto debe tener entre 5 y 300 caracteres.")]
    [PlainText]
    public string Concept { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "La referencia no puede superar los 50 caracteres.")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "La referencia solo admite letras sin tildes, números y guion.")]
    public string? Reference { get; set; }
}

public sealed class AnnulCashMovementRequest
{
    [Required(ErrorMessage = "Indique el motivo de la anulación.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 500 caracteres.")]
    [PlainText]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Conteo de un método distinto del efectivo (vouchers, transferencias).</summary>
public sealed class CashCountRequest
{
    [Required(ErrorMessage = "Indique el método de pago.")]
    [Range(1, int.MaxValue, ErrorMessage = "Indique el método de pago.")]
    public int? PaymentMethodId { get; set; }

    [Required(ErrorMessage = "Indique el monto contado.")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "El monto contado debe estar entre 0 y 10,000,000.")]
    [MaxDecimals(2, "El monto contado admite como máximo 2 decimales.")]
    public decimal? Counted { get; set; }
}

public sealed class CloseCashSessionRequest : IValidatableObject
{
    [Required(ErrorMessage = "Indique el efectivo contado.")]
    [Range(typeof(decimal), "0", "10000000", ErrorMessage = "El efectivo contado debe estar entre 0 y 10,000,000.")]
    [MaxDecimals(2, "El efectivo contado admite como máximo 2 decimales.")]
    public decimal? CountedCash { get; set; }

    [UniqueBy(nameof(CashCountRequest.PaymentMethodId), "Cada método de pago solo puede contarse una vez.")]
    public List<CashCountRequest> Counts { get; set; } = [];

    /// <summary>Obligatorias si el conteo no cuadra con el sistema.</summary>
    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    [PlainText]
    public string? Notes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        NestedValidation.Validate(Counts, nameof(Counts));
}

public sealed class CashMovementResponse
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string TypeCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int Nature { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Concept { get; set; }

    public string? Reference { get; set; }

    public int? SaleId { get; set; }

    public string? SaleNumber { get; set; }

    public string Employee { get; set; } = string.Empty;

    public bool Annulled { get; set; }

    public string? AnnulmentReason { get; set; }

    public bool CanAnnul { get; set; }
}

/// <summary>Totales por método de pago de una apertura (sin anulados).</summary>
public sealed class CashMethodSummaryResponse
{
    public int PaymentMethodId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool AffectsCash { get; set; }

    public decimal Income { get; set; }

    public decimal Expense { get; set; }

    public decimal Net { get; set; }
}

public sealed class CashClosingLineResponse
{
    public string PaymentMethod { get; set; } = string.Empty;

    public decimal System { get; set; }

    public decimal Counted { get; set; }

    public decimal Difference { get; set; }
}

public sealed class CashClosingResponse
{
    public DateTime ClosedAt { get; set; }

    public string ClosedBy { get; set; } = string.Empty;

    public decimal ExpectedCash { get; set; }

    public decimal CountedCash { get; set; }

    /// <summary>Contado − esperado: positivo sobra, negativo falta.</summary>
    public decimal Difference { get; set; }

    public string? Notes { get; set; }

    public IReadOnlyCollection<CashClosingLineResponse> Lines { get; set; } = [];
}

public sealed class CashSessionListItemResponse
{
    public int Id { get; set; }

    public string CashRegister { get; set; } = string.Empty;

    public string Employee { get; set; } = string.Empty;

    public string EmployeeCode { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }

    public bool IsOpen { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal InitialAmount { get; set; }

    public decimal? CountedCash { get; set; }

    public decimal? Difference { get; set; }
}

public sealed class CashSessionResponse
{
    public int Id { get; set; }

    public int CashRegisterId { get; set; }

    public string CashRegister { get; set; } = string.Empty;

    public int EmployeeId { get; set; }

    public string Employee { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; }

    public bool IsOpen { get; set; }

    public string? Notes { get; set; }

    public decimal InitialAmount { get; set; }

    public decimal CashIncome { get; set; }

    public decimal CashExpense { get; set; }

    /// <summary>Inicial + ingresos − egresos en efectivo.</summary>
    public decimal ExpectedCash { get; set; }

    public IReadOnlyCollection<CashMethodSummaryResponse> Methods { get; set; } = [];

    public IReadOnlyCollection<CashMovementResponse> Movements { get; set; } = [];

    public CashClosingResponse? Closing { get; set; }

    /// <summary>La apertura es del usuario actual y sigue abierta.</summary>
    public bool IsMine { get; set; }
}

public sealed class CashSessionQuery
{
    public int? CashRegisterId { get; set; }

    public bool? IsOpen { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
