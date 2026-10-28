using Microsoft.AspNetCore.Mvc;
using TikalLibros.Api.Auditing;
using TikalLibros.Api.Common;
using TikalLibros.Application.Common;
using TikalLibros.Application.DTOs.Categories;
using TikalLibros.Application.DTOs.Common;
using TikalLibros.Application.DTOs.Organization;
using TikalLibros.Application.DTOs.PriceLists;
using TikalLibros.Application.Interfaces;
using TikalLibros.Infrastructure.Security.Permissions;

namespace TikalLibros.Api.Controllers;

/// <summary>
/// Comportamiento común de los catálogos base: listar, consultar, crear,
/// modificar y activar/desactivar. Cada controlador fija la ruta y los
/// permisos de sus acciones.
/// </summary>
[ApiController]
public abstract class AdminCatalogController<TResponse, TSave> : ControllerBase
{
    private readonly IAdminCatalogService<TResponse, TSave> _service;
    private readonly string _notFound;

    protected AdminCatalogController(IAdminCatalogService<TResponse, TSave> service, string notFound)
    {
        _service = service;
        _notFound = notFound;
    }

    protected async Task<ActionResult<IReadOnlyList<TResponse>>> ListAsync(string? search, bool? isActive, CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(search, isActive, cancellationToken));

    protected async Task<ActionResult<TResponse>> FindAsync(int id, CancellationToken cancellationToken)
    {
        TResponse? item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? this.NotFoundProblem(_notFound) : Ok(item);
    }

    protected async Task<ActionResult<TResponse>> CreateItemAsync(TSave request, Func<TResponse, int> id, CancellationToken cancellationToken)
    {
        OperationResult<TResponse> result = await _service.CreateAsync(request, cancellationToken);

        return result.Succeeded
            ? CreatedAtAction("GetById", new { id = id(result.Data!) }, result.Data)
            : this.OperationProblem(result);
    }

    protected async Task<ActionResult<TResponse>> UpdateItemAsync(int id, TSave request, CancellationToken cancellationToken)
    {
        OperationResult<TResponse> result = await _service.UpdateAsync(id, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }

    protected async Task<ActionResult<TResponse>> ChangeItemStatusAsync(int id, ChangeActiveStatusRequest request, CancellationToken cancellationToken)
    {
        OperationResult<TResponse> result = await _service.ChangeStatusAsync(id, request.IsActive, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : this.OperationProblem(result);
    }
}

/// <summary>Categorías y subcategorías del catálogo. Usa los permisos de Catálogo.</summary>
[AuditEntity("Categoria")]
[Route("api/categorias")]
public sealed class CategoriesController(ICategoryService service)
    : AdminCatalogController<CategoryResponse, SaveCategoryRequest>(service, "La categoría solicitada no existe.")
{
    [HasPermission(CatalogPermissions.View)]
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<CategoryResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        ListAsync(search, isActive, cancellationToken);

    [HasPermission(CatalogPermissions.View)]
    [HttpGet("{id:int}")]
    public Task<ActionResult<CategoryResponse>> GetById(int id, CancellationToken cancellationToken) => FindAsync(id, cancellationToken);

    [HasPermission(CatalogPermissions.Create)]
    [HttpPost]
    public Task<ActionResult<CategoryResponse>> Create([FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        CreateItemAsync(request, item => item.Id, cancellationToken);

    [HasPermission(CatalogPermissions.Update)]
    [HttpPut("{id:int}")]
    public Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] SaveCategoryRequest request, CancellationToken cancellationToken) =>
        UpdateItemAsync(id, request, cancellationToken);

    [HasPermission(CatalogPermissions.Disable)]
    [HttpPatch("{id:int}/estado")]
    public Task<ActionResult<CategoryResponse>> ChangeStatus(int id, [FromBody] ChangeActiveStatusRequest request, CancellationToken cancellationToken) =>
        ChangeItemStatusAsync(id, request, cancellationToken);
}

/// <summary>Listas de precio. Consultar requiere Catálogo.Ver; modificarlas, administrar precios.</summary>
[AuditEntity("ListaPrecio")]
[Route("api/listas-precio")]
public sealed class PriceListsController(IPriceListService service)
    : AdminCatalogController<PriceListResponse, SavePriceListRequest>(service, "La lista de precios solicitada no existe.")
{
    [HasPermission(CatalogPermissions.View)]
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<PriceListResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        ListAsync(search, isActive, cancellationToken);

    [HasPermission(CatalogPermissions.View)]
    [HttpGet("{id:int}")]
    public Task<ActionResult<PriceListResponse>> GetById(int id, CancellationToken cancellationToken) => FindAsync(id, cancellationToken);

    [HasPermission(CatalogPermissions.ManagePrices)]
    [HttpPost]
    public Task<ActionResult<PriceListResponse>> Create([FromBody] SavePriceListRequest request, CancellationToken cancellationToken) =>
        CreateItemAsync(request, item => item.Id, cancellationToken);

    [HasPermission(CatalogPermissions.ManagePrices)]
    [HttpPut("{id:int}")]
    public Task<ActionResult<PriceListResponse>> Update(int id, [FromBody] SavePriceListRequest request, CancellationToken cancellationToken) =>
        UpdateItemAsync(id, request, cancellationToken);

    [HasPermission(CatalogPermissions.ManagePrices)]
    [HttpPatch("{id:int}/estado")]
    public Task<ActionResult<PriceListResponse>> ChangeStatus(int id, [FromBody] ChangeActiveStatusRequest request, CancellationToken cancellationToken) =>
        ChangeItemStatusAsync(id, request, cancellationToken);
}

/// <summary>Sucursales de la empresa (Configuración &gt; Organización).</summary>
[AuditEntity("Sucursal")]
[Route("api/sucursales")]
[HasPermission(ConfigurationPermissions.ManageOrganization)]
public sealed class BranchesController(IBranchService service)
    : AdminCatalogController<BranchResponse, SaveBranchRequest>(service, "La sucursal solicitada no existe.")
{
    [HttpGet("catalogos")]
    public async Task<ActionResult<OrganizationCatalogsResponse>> GetCatalogs(CancellationToken cancellationToken) =>
        Ok(await service.GetCatalogsAsync(cancellationToken));

    [HttpGet]
    public Task<ActionResult<IReadOnlyList<BranchResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        ListAsync(search, isActive, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ActionResult<BranchResponse>> GetById(int id, CancellationToken cancellationToken) => FindAsync(id, cancellationToken);

    [HttpPost]
    public Task<ActionResult<BranchResponse>> Create([FromBody] SaveBranchRequest request, CancellationToken cancellationToken) =>
        CreateItemAsync(request, item => item.Id, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ActionResult<BranchResponse>> Update(int id, [FromBody] SaveBranchRequest request, CancellationToken cancellationToken) =>
        UpdateItemAsync(id, request, cancellationToken);

    [HttpPatch("{id:int}/estado")]
    public Task<ActionResult<BranchResponse>> ChangeStatus(int id, [FromBody] ChangeActiveStatusRequest request, CancellationToken cancellationToken) =>
        ChangeItemStatusAsync(id, request, cancellationToken);
}

/// <summary>Almacenes de cada sucursal (Configuración &gt; Organización).</summary>
[AuditEntity("Almacen")]
[Route("api/almacenes")]
[HasPermission(ConfigurationPermissions.ManageOrganization)]
public sealed class WarehousesController(IWarehouseService service)
    : AdminCatalogController<WarehouseResponse, SaveWarehouseRequest>(service, "El almacén solicitado no existe.")
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<WarehouseResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        ListAsync(search, isActive, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ActionResult<WarehouseResponse>> GetById(int id, CancellationToken cancellationToken) => FindAsync(id, cancellationToken);

    [HttpPost]
    public Task<ActionResult<WarehouseResponse>> Create([FromBody] SaveWarehouseRequest request, CancellationToken cancellationToken) =>
        CreateItemAsync(request, item => item.Id, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ActionResult<WarehouseResponse>> Update(int id, [FromBody] SaveWarehouseRequest request, CancellationToken cancellationToken) =>
        UpdateItemAsync(id, request, cancellationToken);

    [HttpPatch("{id:int}/estado")]
    public Task<ActionResult<WarehouseResponse>> ChangeStatus(int id, [FromBody] ChangeActiveStatusRequest request, CancellationToken cancellationToken) =>
        ChangeItemStatusAsync(id, request, cancellationToken);
}

/// <summary>Cajas de cada sucursal (Configuración &gt; Organización).</summary>
[AuditEntity("Caja")]
[Route("api/cajas")]
[HasPermission(ConfigurationPermissions.ManageOrganization)]
public sealed class CashRegistersController(ICashRegisterService service)
    : AdminCatalogController<CashRegisterAdminResponse, SaveCashRegisterRequest>(service, "La caja solicitada no existe.")
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<CashRegisterAdminResponse>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive, CancellationToken cancellationToken) =>
        ListAsync(search, isActive, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ActionResult<CashRegisterAdminResponse>> GetById(int id, CancellationToken cancellationToken) => FindAsync(id, cancellationToken);

    [HttpPost]
    public Task<ActionResult<CashRegisterAdminResponse>> Create([FromBody] SaveCashRegisterRequest request, CancellationToken cancellationToken) =>
        CreateItemAsync(request, item => item.Id, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<ActionResult<CashRegisterAdminResponse>> Update(int id, [FromBody] SaveCashRegisterRequest request, CancellationToken cancellationToken) =>
        UpdateItemAsync(id, request, cancellationToken);

    [HttpPatch("{id:int}/estado")]
    public Task<ActionResult<CashRegisterAdminResponse>> ChangeStatus(int id, [FromBody] ChangeActiveStatusRequest request, CancellationToken cancellationToken) =>
        ChangeItemStatusAsync(id, request, cancellationToken);
}
