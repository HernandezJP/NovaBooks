namespace TikalLibros.Application.Common;

/// <summary>Parámetro configurable de la empresa (PB_PARAMETRO_SISTEMA).</summary>
public sealed record SystemParameterDefinition(string Key, string DataType, string DefaultValue, string Description);

/// <summary>Claves de los parámetros del sistema y sus valores iniciales.</summary>
public static class SystemParameterKeys
{
    /// <summary>Porcentaje de descuento por línea que se aplica sin aprobación.</summary>
    public const string DiscountWithoutApproval = "VENTAS_DESCUENTO_SIN_APROBACION";

    /// <summary>Descuento máximo por línea, aun con aprobación.</summary>
    public const string MaximumDiscount = "VENTAS_DESCUENTO_MAXIMO";

    /// <summary>Días que un pedido reservado aparta la mercadería.</summary>
    public const string ReservationDays = "PEDIDOS_DIAS_RESERVA";

    /// <summary>NIT de la empresa que aparece como emisor en las facturas.</summary>
    public const string InvoiceIssuerNit = "FACTURA_NIT_EMISOR";

    public static readonly IReadOnlyList<SystemParameterDefinition> Defaults =
    [
        new(DiscountWithoutApproval, "DECIMAL", "10",
            "Porcentaje de descuento por línea que puede aplicarse sin aprobación de un supervisor."),
        new(MaximumDiscount, "DECIMAL", "50",
            "Porcentaje máximo de descuento por línea, aun con aprobación."),
        new(ReservationDays, "INTEGER", "3",
            "Días que un pedido reservado aparta la mercadería antes de vencer."),
        new(InvoiceIssuerNit, "STRING", "4567890K",
            "NIT de la empresa como emisor de las facturas electrónicas.")
    ];
}
