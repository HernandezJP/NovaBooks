namespace TikalLibros.Application.DTOs.Sales;

/// <summary>Códigos de estado de un pedido de cliente.</summary>
public static class OrderStatus
{
    public const string Draft = "BORRADOR";

    /// <summary>Descuento mayor al permitido o crédito excedido: espera a un supervisor.</summary>
    public const string PendingApproval = "POR_APROBAR";

    /// <summary>Aprobado o sin necesidad de aprobación: la mercadería está apartada.</summary>
    public const string Reserved = "RESERVADO";

    public const string Invoiced = "FACTURADO";
    public const string Rejected = "RECHAZADO";
    public const string Cancelled = "CANCELADO";

    /// <summary>La reserva venció sin facturarse y la mercadería se liberó.</summary>
    public const string Expired = "VENCIDO";
}

/// <summary>Códigos de estado de una venta.</summary>
public static class SaleStatus
{
    public const string Paid = "PAGADA";
    public const string Partial = "PARCIAL";
    public const string Credit = "CREDITO";
    public const string Annulled = "ANULADA";

    /// <summary>Todas las unidades se devolvieron.</summary>
    public const string Returned = "DEVUELTA";
}

/// <summary>Códigos de tipo de venta.</summary>
public static class SaleType
{
    public const string Cash = "CONTADO";
    public const string Credit = "CREDITO";
}

/// <summary>Códigos de método de pago.</summary>
public static class PaymentMethodCode
{
    public const string Cash = "EFECTIVO";
    public const string Card = "TARJETA";
    public const string Transfer = "TRANSFERENCIA";
    public const string Credit = "CREDITO";
}
