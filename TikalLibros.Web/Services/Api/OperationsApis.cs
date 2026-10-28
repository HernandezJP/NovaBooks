using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Employees;
using TikalLibros.Application.DTOs.Inventory;
using TikalLibros.Application.DTOs.Purchasing;
using TikalLibros.Application.DTOs.Suppliers;
using TikalLibros.Application.DTOs.Users;

namespace TikalLibros.Web.Services.Api;

public sealed class EmployeesApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<EmployeeListItemResponse>>> GetPagedAsync(
        string? search, StatusFilter status, int? positionId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<EmployeeListItemResponse>>(
            QueryString.Build("api/empleados",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("positionId", positionId),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<EmployeeResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<EmployeeResponse>($"api/empleados/{id}", cancellationToken);

    public Task<ApiResult<EmployeeCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<EmployeeCatalogsResponse>("api/empleados/catalogos", cancellationToken);

    public Task<ApiResult<EmployeeResponse>> CreateAsync(SaveEmployeeRequest request) =>
        api.PostAsync<EmployeeResponse>("api/empleados", request);

    public Task<ApiResult<EmployeeResponse>> UpdateAsync(int id, SaveEmployeeRequest request) =>
        api.PutAsync<EmployeeResponse>($"api/empleados/{id}", request);

    public Task<ApiResult<EmployeeResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<EmployeeResponse>($"api/empleados/{id}/estado", new ChangeEmployeeStatusRequest { IsActive = isActive });

    public Task<ApiResult<UserResponse>> CreateUserAsync(CreateUserRequest request) =>
        api.PostAsync<UserResponse>("api/usuarios", request);

    public Task<ApiResult<UserResponse>> AssignUserAsync(int userId, int? employeeId) =>
        api.PutAsync<UserResponse>($"api/usuarios/{userId}/empleado", new AssignUserEmployeeRequest { EmployeeId = employeeId });
}

public sealed class SuppliersApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<SupplierListItemResponse>>> GetPagedAsync(
        string? search, StatusFilter status, string? personType, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<SupplierListItemResponse>>(
            QueryString.Build("api/proveedores",
                ("search", search?.Trim()),
                ("isActive", QueryString.ToIsActive(status)),
                ("personType", personType),
                ("page", page),
                ("pageSize", pageSize)),
            cancellationToken);

    public Task<ApiResult<SupplierResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<SupplierResponse>($"api/proveedores/{id}", cancellationToken);

    public Task<ApiResult<SupplierCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<SupplierCatalogsResponse>("api/proveedores/catalogos", cancellationToken);

    public Task<ApiResult<SupplierResponse>> CreateAsync(SaveSupplierRequest request) =>
        api.PostAsync<SupplierResponse>("api/proveedores", request);

    public Task<ApiResult<SupplierResponse>> UpdateAsync(int id, SaveSupplierRequest request) =>
        api.PutAsync<SupplierResponse>($"api/proveedores/{id}", request);

    public Task<ApiResult<SupplierResponse>> ChangeStatusAsync(int id, bool isActive) =>
        api.PatchAsync<SupplierResponse>($"api/proveedores/{id}/estado", new ChangeSupplierStatusRequest { IsActive = isActive });
}

public sealed class PurchasesApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<PurchaseOrderListItemResponse>>> GetPagedAsync(
        PurchaseOrderQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<PurchaseOrderListItemResponse>>(
            QueryString.Build("api/compras",
                ("search", query.Search?.Trim()),
                ("status", query.Status),
                ("supplierId", query.SupplierId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<PurchaseOrderResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<PurchaseOrderResponse>($"api/compras/{id}", cancellationToken);

    public Task<ApiResult<PurchaseOrderCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<PurchaseOrderCatalogsResponse>("api/compras/catalogos", cancellationToken);

    public Task<ApiResult<List<PurchaseBookOptionResponse>>> SearchBooksAsync(string? search, CancellationToken cancellationToken = default) =>
        api.GetAsync<List<PurchaseBookOptionResponse>>(QueryString.Build("api/compras/libros", ("search", search?.Trim())), cancellationToken);

    public Task<ApiResult<PurchaseOrderResponse>> CreateAsync(SavePurchaseOrderRequest request) =>
        api.PostAsync<PurchaseOrderResponse>("api/compras", request);

    public Task<ApiResult<PurchaseOrderResponse>> UpdateAsync(int id, SavePurchaseOrderRequest request) =>
        api.PutAsync<PurchaseOrderResponse>($"api/compras/{id}", request);

    public Task<ApiResult<PurchaseOrderResponse>> IssueAsync(int id) =>
        api.PostAsync<PurchaseOrderResponse>($"api/compras/{id}/emitir", new { });

    public Task<ApiResult<PurchaseOrderResponse>> CancelAsync(int id, string reason) =>
        api.PostAsync<PurchaseOrderResponse>($"api/compras/{id}/cancelar", new CancelPurchaseOrderRequest { Reason = reason });
}

public sealed class ReceptionsApi(ApiClient api)
{
    public Task<ApiResult<PagedResponse<ReceptionListItemResponse>>> GetPagedAsync(
        ReceptionQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<ReceptionListItemResponse>>(
            QueryString.Build("api/recepciones",
                ("search", query.Search?.Trim()),
                ("status", query.Status),
                ("purchaseOrderId", query.PurchaseOrderId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<ReceptionResponse>> GetAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ReceptionResponse>($"api/recepciones/{id}", cancellationToken);

    public Task<ApiResult<ReceptionCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<ReceptionCatalogsResponse>("api/recepciones/catalogos", cancellationToken);

    public Task<ApiResult<ReceptionResponse>> RegisterAsync(RegisterReceptionRequest request) =>
        api.PostAsync<ReceptionResponse>("api/recepciones", request);

    public Task<ApiResult<ReceptionResponse>> RegisterDirectAsync(DirectPurchaseRequest request) =>
        api.PostAsync<ReceptionResponse>("api/recepciones/compra-directa", request);

    public Task<ApiResult<ReceptionResponse>> AnnulAsync(int id, string reason) =>
        api.PostAsync<ReceptionResponse>($"api/recepciones/{id}/anular", new AnnulReceptionRequest { Reason = reason });
}

public sealed class InventoryApi(ApiClient api)
{
    public Task<ApiResult<InventoryCatalogsResponse>> GetCatalogsAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<InventoryCatalogsResponse>("api/inventario/catalogos", cancellationToken);

    public Task<ApiResult<PagedResponse<StockItemResponse>>> GetStockAsync(StockQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<StockItemResponse>>(
            QueryString.Build("api/inventario/existencias",
                ("search", query.Search?.Trim()),
                ("warehouseId", query.WarehouseId),
                ("status", query.Status),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<StockItemResponse>> SaveLevelsAsync(SaveStockLevelsRequest request) =>
        api.PutAsync<StockItemResponse>("api/inventario/existencias/niveles", request);

    public Task<ApiResult<List<StockAlertResponse>>> GetAlertsAsync(int? warehouseId, CancellationToken cancellationToken = default) =>
        api.GetAsync<List<StockAlertResponse>>(QueryString.Build("api/inventario/alertas", ("warehouseId", warehouseId)), cancellationToken);

    public Task<ApiResult<StockAlertSummaryResponse>> GetAlertSummaryAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<StockAlertSummaryResponse>("api/inventario/alertas/resumen", cancellationToken);

    public Task<ApiResult<PagedResponse<MovementListItemResponse>>> GetMovementsAsync(MovementQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<MovementListItemResponse>>(
            QueryString.Build("api/inventario/movimientos",
                ("search", query.Search?.Trim()),
                ("type", query.Type),
                ("warehouseId", query.WarehouseId),
                ("bookId", query.BookId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("includeAnnulled", query.IncludeAnnulled),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<MovementResponse>> GetMovementAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<MovementResponse>($"api/inventario/movimientos/{id}", cancellationToken);

    public Task<ApiResult<KardexResponse>> GetKardexAsync(KardexQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<KardexResponse>(
            QueryString.Build("api/inventario/kardex",
                ("bookId", query.BookId),
                ("warehouseId", query.WarehouseId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd"))),
            cancellationToken);

    public Task<ApiResult<PagedResponse<AdjustmentListItemResponse>>> GetAdjustmentsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<AdjustmentListItemResponse>>(
            QueryString.Build("api/inventario/ajustes", ("search", search?.Trim()), ("page", page), ("pageSize", pageSize)), cancellationToken);

    public Task<ApiResult<AdjustmentResponse>> GetAdjustmentAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<AdjustmentResponse>($"api/inventario/ajustes/{id}", cancellationToken);

    public Task<ApiResult<AdjustmentResponse>> CreateAdjustmentAsync(SaveAdjustmentRequest request) =>
        api.PostAsync<AdjustmentResponse>("api/inventario/ajustes", request);

    public Task<ApiResult<PagedResponse<TransferListItemResponse>>> GetTransfersAsync(TransferQuery query, CancellationToken cancellationToken = default) =>
        api.GetAsync<PagedResponse<TransferListItemResponse>>(
            QueryString.Build("api/inventario/traslados",
                ("search", query.Search?.Trim()),
                ("warehouseId", query.WarehouseId),
                ("from", query.From?.ToString("yyyy-MM-dd")),
                ("to", query.To?.ToString("yyyy-MM-dd")),
                ("includeAnnulled", query.IncludeAnnulled),
                ("page", query.Page),
                ("pageSize", query.PageSize)),
            cancellationToken);

    public Task<ApiResult<MovementResponse>> GetTransferAsync(int id, CancellationToken cancellationToken = default) =>
        api.GetAsync<MovementResponse>($"api/inventario/traslados/{id}", cancellationToken);

    public Task<ApiResult<MovementResponse>> CreateTransferAsync(SaveTransferRequest request) =>
        api.PostAsync<MovementResponse>("api/inventario/traslados", request);

    public Task<ApiResult<MovementResponse>> AnnulTransferAsync(int id, string reason) =>
        api.PostAsync<MovementResponse>($"api/inventario/traslados/{id}/anular", new AnnulTransferRequest { Reason = reason });
}
