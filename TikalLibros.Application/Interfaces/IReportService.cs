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
}
