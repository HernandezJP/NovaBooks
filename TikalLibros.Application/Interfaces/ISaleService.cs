using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.Application.Interfaces;

public interface ISaleService
{
    Task<SaleCatalogsResponse> GetCatalogsAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResponse<SaleListItemResponse>> GetPagedAsync(
        SaleQuery query,
        CancellationToken cancellationToken = default);

    Task<SaleResponse?> GetByIdAsync(
        int id,
        bool includeCosts,
        CancellationToken cancellationToken = default);

    /// <summary>Factura un pedido reservado: cobra, descuenta inventario y libera la reserva.</summary>
    Task<OperationResult<SaleResponse>> InvoiceOrderAsync(
        int orderId,
        InvoiceOrderRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    /// <summary>Venta de mostrador sin pedido previo.</summary>
    Task<OperationResult<SaleResponse>> SellAsync(
        DirectSaleRequest request,
        int authenticatedUserId,
        SalesPermissionSet permissions,
        CancellationToken cancellationToken = default);

    /// <summary>Abono a una venta al crédito o pagada parcialmente.</summary>
    Task<OperationResult<SaleResponse>> AddPaymentsAsync(
        int saleId,
        AddPaymentsRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Factura de la venta para mostrarla o imprimirla.</summary>
    Task<InvoiceResponse?> GetInvoiceAsync(
        int saleId,
        CancellationToken cancellationToken = default);

    Task<OperationResult<SaleResponse>> AnnulAsync(
        int saleId,
        AnnulSaleRequest request,
        int authenticatedUserId,
        CancellationToken cancellationToken = default);
}
