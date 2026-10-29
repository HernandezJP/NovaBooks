using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Reports;

namespace TikalLibros.Application.Interfaces;

public interface IReportService
{
    /// <summary>Ventas del período: totales, por día, vendedor, método de pago y condición.</summary>
    Task<OperationResult<SalesReportResponse>> GetSalesAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    Task<OperationResult<TopBooksResponse>> GetTopBooksAsync(
        TopBooksQuery query,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    Task<InventoryValuationResponse> GetInventoryValuationAsync(
        InventoryValuationQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Pedidos por aprobar, reservados y en borrador, con su antigüedad.</summary>
    Task<PendingOrdersResponse> GetPendingOrdersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Recepciones, devoluciones al proveedor y órdenes por recibir.</summary>
    Task<OperationResult<PurchasesReportResponse>> GetPurchasesAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Aperturas del período con su cuadre y movimientos por tipo y método.</summary>
    Task<OperationResult<CashReportResponse>> GetCashAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Ingreso, costo y margen de lo vendido, descontando devoluciones.</summary>
    Task<OperationResult<ProfitabilityReportResponse>> GetProfitabilityAsync(
        ReportPeriodQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Saldos de ventas al crédito por antigüedad, a la fecha.</summary>
    Task<ReceivablesResponse> GetReceivablesAsync(
        ReceivablesQuery query,
        CancellationToken cancellationToken = default);
}
