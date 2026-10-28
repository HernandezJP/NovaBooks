using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Audit;
using TikalLibros.Application.DTOs.Cash;
using TikalLibros.Application.DTOs.Configuration;
using TikalLibros.Application.DTOs.Reports;
using TikalLibros.Application.DTOs.Sales;

namespace TikalLibros.Web.Services.Api;

public sealed class CashApi(ApiClient api)
{
    public Task<ApiResult<CashCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<CashCatalogsResponse>("api/caja/catalogos", cancellationToken);

    /// <summary>La caja abierta del usuario; Data es null si no tiene ninguna.</summary>
    public Task<ApiResult<CashSessionResponse>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<CashSessionResponse>("api/caja/aperturas/actual", cancellationToken);

    public Task<ApiResult<PagedResponse<CashSessionListItemResponse>>> GetSessionsAsync(
        CashSessionQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<CashSessionListItemResponse>>(
            QueryString.Build("api/caja/aperturas",
                ("cashRegisterId", query.CashRegisterId),
                ("isOpen", query.IsOpen),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<CashSessionResponse>> GetSessionAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<CashSessionResponse>($"api/caja/aperturas/{id}", cancellationToken);

    public Task<ApiResult<CashSessionResponse>> OpenAsync(OpenCashSessionRequest request) =>
        api.PostAsync<CashSessionResponse>("api/caja/aperturas", request);

    public Task<ApiResult<CashSessionResponse>> RegisterMovementAsync(int sessionId, CashMovementRequest request) =>
        api.PostAsync<CashSessionResponse>($"api/caja/aperturas/{sessionId}/movimientos", request);

    public Task<ApiResult<CashSessionResponse>> AnnulMovementAsync(int movementId, string reason) =>
        api.PostAsync<CashSessionResponse>($"api/caja/movimientos/{movementId}/anular", new AnnulCashMovementRequest { Reason = reason });

    public Task<ApiResult<CashSessionResponse>> CloseAsync(int sessionId, CloseCashSessionRequest request) =>
        api.PostAsync<CashSessionResponse>($"api/caja/aperturas/{sessionId}/cerrar", request);
}

public sealed class OrdersApi(ApiClient api)
{
    public Task<ApiResult<OrderCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<OrderCatalogsResponse>("api/pedidos/catalogos", cancellationToken);

    public Task<ApiResult<List<SalesBookOptionResponse>>> SearchBooksAsync(
        string? search, int? priceListId, int? warehouseId, CancellationToken cancellationToken = default) =>
        api.GetAsync<List<SalesBookOptionResponse>>(
            QueryString.Build("api/pedidos/libros",
                ("search", search?.Trim()),
                ("priceListId", priceListId),
                ("warehouseId", warehouseId)),
            cancellationToken);

    public Task<ApiResult<PagedResponse<OrderListItemResponse>>> GetPagedAsync(OrderQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<OrderListItemResponse>>(
            QueryString.Build("api/pedidos",
                ("search", query.Search?.Trim()),
                ("status", query.Status),
                ("customerId", query.CustomerId),
                ("saleType", query.SaleType),
                ("employeeId", query.EmployeeId),
                ("bookId", query.BookId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("minTotal", query.MinTotal),
                ("maxTotal", query.MaxTotal),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<OrderResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<OrderResponse>($"api/pedidos/{id}", cancellationToken);

    public Task<ApiResult<OrderResponse>> CreateAsync(SaveOrderRequest request) =>
        api.PostAsync<OrderResponse>("api/pedidos", request);

    public Task<ApiResult<OrderResponse>> UpdateAsync(int id, SaveOrderRequest request) =>
        api.PutAsync<OrderResponse>($"api/pedidos/{id}", request);

    public Task<ApiResult<OrderResponse>> ConfirmAsync(int id) =>
        api.PostAsync<OrderResponse>($"api/pedidos/{id}/confirmar", new { });

    public Task<ApiResult<OrderResponse>> ApproveAsync(int id, string? comment) =>
        api.PostAsync<OrderResponse>($"api/pedidos/{id}/aprobar", new ApproveOrderRequest { Comment = comment });

    public Task<ApiResult<OrderResponse>> RejectAsync(int id, string reason) =>
        api.PostAsync<OrderResponse>($"api/pedidos/{id}/rechazar", new OrderReasonRequest { Reason = reason });

    public Task<ApiResult<OrderResponse>> CancelAsync(int id, string reason) =>
        api.PostAsync<OrderResponse>($"api/pedidos/{id}/cancelar", new OrderReasonRequest { Reason = reason });
}

public sealed class SalesApi(ApiClient api)
{
    public Task<ApiResult<SaleCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<SaleCatalogsResponse>("api/ventas/catalogos", cancellationToken);

    public Task<ApiResult<PagedResponse<SaleListItemResponse>>> GetPagedAsync(SaleQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<SaleListItemResponse>>(
            QueryString.Build("api/ventas",
                ("search", query.Search?.Trim()),
                ("status", query.Status),
                ("saleType", query.SaleType),
                ("customerId", query.CustomerId),
                ("employeeId", query.EmployeeId),
                ("bookId", query.BookId),
                ("paymentMethodId", query.PaymentMethodId),
                ("withBalance", query.WithBalance),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("minTotal", query.MinTotal),
                ("maxTotal", query.MaxTotal),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<SaleResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<SaleResponse>($"api/ventas/{id}", cancellationToken);

    public Task<ApiResult<InvoiceResponse>> GetInvoiceAsync(int saleId, CancellationToken cancellationToken = default) =>
        api.GetAsync<InvoiceResponse>($"api/ventas/{saleId}/factura", cancellationToken);

    public Task<ApiResult<SaleResponse>> SellAsync(DirectSaleRequest request) =>
        api.PostAsync<SaleResponse>("api/ventas", request);

    public Task<ApiResult<SaleResponse>> InvoiceOrderAsync(int orderId, InvoiceOrderRequest request) =>
        api.PostAsync<SaleResponse>($"api/ventas/desde-pedido/{orderId}", request);

    public Task<ApiResult<SaleResponse>> AddPaymentsAsync(int saleId, AddPaymentsRequest request) =>
        api.PostAsync<SaleResponse>($"api/ventas/{saleId}/pagos", request);

    public Task<ApiResult<SaleResponse>> AnnulAsync(int saleId, string reason) =>
        api.PostAsync<SaleResponse>($"api/ventas/{saleId}/anular", new AnnulSaleRequest { Reason = reason });
}

public sealed class ReturnsApi(ApiClient api)
{
    public Task<ApiResult<ReturnCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<ReturnCatalogsResponse>("api/devoluciones/catalogos", cancellationToken);

    public Task<ApiResult<PagedResponse<ReturnListItemResponse>>> GetPagedAsync(ReturnQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<ReturnListItemResponse>>(
            QueryString.Build("api/devoluciones",
                ("search", query.Search?.Trim()),
                ("saleId", query.SaleId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("includeAnnulled", query.IncludeAnnulled),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<ReturnResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ReturnResponse>($"api/devoluciones/{id}", cancellationToken);

    public Task<ApiResult<ReturnResponse>> CreateAsync(SaveReturnRequest request) =>
        api.PostAsync<ReturnResponse>("api/devoluciones", request);

    public Task<ApiResult<ReturnResponse>> AnnulAsync(int id, string reason) =>
        api.PostAsync<ReturnResponse>($"api/devoluciones/{id}/anular", new AnnulReturnRequest { Reason = reason });
}

public sealed class ReportsApi(ApiClient api)
{
    private static (string, object?)[] Period(ReportPeriodQuery query) =>
    [
        ("from", query.From?.ToString("yyyy-MM-dd")),
        ("to", query.To?.ToString("yyyy-MM-dd")),
        ("warehouseId", query.WarehouseId)
    ];

    public Task<ApiResult<SalesReportResponse>> GetSalesAsync(ReportPeriodQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<SalesReportResponse>(QueryString.Build("api/reportes/ventas", Period(query)), cancellationToken);

    public Task<ApiResult<TopBooksResponse>> GetTopBooksAsync(TopBooksQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<TopBooksResponse>(
            QueryString.Build("api/reportes/libros-mas-vendidos", [.. Period(query), ("top", query.Top), ("orderBy", query.OrderBy)]),
            cancellationToken);

    public Task<ApiResult<InventoryValuationResponse>> GetInventoryValuationAsync(
        InventoryValuationQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<InventoryValuationResponse>(
            QueryString.Build("api/reportes/inventario-valorizado",
                ("warehouseId", query.WarehouseId),
                ("search", query.Search?.Trim()),
                ("status", query.Status),
                ("onlyWithStock", query.OnlyWithStock ? true : null)),
            cancellationToken);

    public Task<ApiResult<PendingOrdersResponse>> GetPendingOrdersAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<PendingOrdersResponse>("api/reportes/pedidos-pendientes", cancellationToken);

    public Task<ApiResult<PurchasesReportResponse>> GetPurchasesAsync(ReportPeriodQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PurchasesReportResponse>(QueryString.Build("api/reportes/compras", Period(query)), cancellationToken);

    public Task<ApiResult<CashReportResponse>> GetCashAsync(ReportPeriodQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<CashReportResponse>(QueryString.Build("api/reportes/caja", Period(query)), cancellationToken);

    public Task<ApiResult<ProfitabilityReportResponse>> GetProfitabilityAsync(ReportPeriodQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<ProfitabilityReportResponse>(QueryString.Build("api/reportes/rentabilidad", Period(query)), cancellationToken);

    public Task<ApiResult<ReceivablesResponse>> GetReceivablesAsync(ReceivablesQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<ReceivablesResponse>(
            QueryString.Build("api/reportes/cuentas-por-cobrar",
                ("customerId", query.CustomerId),
                ("onlyOverdue", query.OnlyOverdue ? true : null)),
            cancellationToken);
}

public sealed class AuditApi(ApiClient api)
{
    public Task<ApiResult<AuditCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<AuditCatalogsResponse>("api/auditoria/catalogos", cancellationToken);

    public Task<ApiResult<PagedResponse<AuditEntryResponse>>> GetPagedAsync(AuditQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<AuditEntryResponse>>(
            QueryString.Build("api/auditoria",
                ("search", query.Search?.Trim()),
                ("userId", query.UserId),
                ("action", query.Action),
                ("entity", query.Entity),
                ("entityId", query.EntityId?.Trim()),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<AuditDetailResponse>> GetAsync(long id, CancellationToken cancellationToken = default) =>
        api.GetAsync<AuditDetailResponse>($"api/auditoria/{id}", cancellationToken);

    public Task<ApiResult<PagedResponse<AccessLogResponse>>> GetAccessLogAsync(AccessLogQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<AccessLogResponse>>(
            QueryString.Build("api/auditoria/accesos",
                ("userName", query.UserName?.Trim()),
                ("succeeded", query.Succeeded),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);
}

public sealed class ParametersApi(ApiClient api)
{
    public Task<ApiResult<List<SystemParameterResponse>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<List<SystemParameterResponse>>("api/parametros", cancellationToken);

    public Task<ApiResult<SystemParameterResponse>> UpdateAsync(string key, string value) =>
        api.PutAsync<SystemParameterResponse>($"api/parametros/{Uri.EscapeDataString(key)}", new UpdateSystemParameterRequest { Value = value });
}
